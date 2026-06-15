using APP.Framework;
using AppAI.Web.Auth;
using AppAI.Web.Endpoints;
using AppAI.Web.Hubs;
using AppAI.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NLog;
using NLog.Web;
using SD.LLBLGen.Pro.ORMSupportClasses;

// Early NLog init for startup logging
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("AppAI.Web starting — {0}", DateTime.UtcNow);

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ───────────────────────────────────────────────────────────────
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ── Controllers (Newtonsoft.Json matching legacy PascalCase + NullValueHandling.Include) ──
    // DefaultContractResolver preserves C# PascalCase property names in JSON output,
    // matching the old ASP.NET Web API 2 behavior that all client-side code was written for.
    // Without this, AddNewtonsoftJson defaults to CamelCasePropertyNamesContractResolver,
    // which breaks every client-side check like IsLoginFailed, SessionId, DictEnumApp, etc.
    builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
            options.SerializerSettings.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include;
        });

    // ── SignalR ───────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration["CorsAllowedOrigins"] ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(allowedOrigins))
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AppAiCors", policy =>
                policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                      .AllowAnyHeader()
                      .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                      .AllowCredentials());
        });
    }

    // ── Session ───────────────────────────────────────────────────────────────
    // Production: swap AddDistributedMemoryCache → AddStackExchangeRedisCache
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(2880);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // ── Auth ──────────────────────────────────────────────────────────────────
    // Session-token auth (legacy) is enforced via SessionValidationFilter on each action.
    // Cookie auth registration is additive — required by Sustainsys.Saml2.
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();

    builder.Services.AddHttpContextAccessor();

    // SessionValidationFilter replaces SecureBaseWebApiController.Initialize()
    builder.Services.AddScoped<SessionValidationFilter>();

    // IAppClientContext — scoped per-request context (Phase 5: delegates to ServerContext.Instance)
    builder.Services.AddScoped<IAppClientContext, HttpAppClientContext>();

    // IOcrService — LLM-vision OCR (replaces Tesseract; Phase 6)
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<IOcrService, LLMOcrService>();

    // Exception filters (replaces WebApiConfig filter registration)
    // SuppressImplicitRequiredAttributeForNonNullableReferenceTypes restores old Web API 2 behaviour:
    // non-nullable reference properties in DTOs are NOT implicitly [Required].
    // These DTOs predate nullable reference types and have no [Required] annotations.
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<CustomExceptionFilter>();
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    // Health checks — SQL Server probe on AppMasterDB
    var masterConnStr = builder.Configuration.GetConnectionString("AppMasterDBConnectionString") ?? string.Empty;
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString: masterConnStr,
            name: "appmasterdb",
            failureStatus: HealthStatus.Degraded,
            tags: ["db", "sql"]);


    // ── HTTP request logging (Development only) ───────────────────────────────
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestHeaders
                | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode;
        });
    }

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // Redirect bare root → /appai/health so the host status is visible at localhost:<port>/
    app.MapGet("/", () => Results.Redirect("/appai/health")).ExcludeFromDescription();

    // Match the /appai path prefix used by the old IIS deployment and React proxy.
    // UseRouting MUST be called explicitly here — if omitted, ASP.NET Core auto-inserts it
    // before UsePathBase, so the router sees "/appai/health" instead of "/health".
    app.UsePathBase("/appai");
    app.UseRouting();

    // ── One-time startup calls (replacing Global.asax Application_Start) ──────
    // Wire IHttpContextAccessor into the legacy static ServerContext, then initialise
    // the identity providers — mirrors Global.asax Application_Start in the old host.
    var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
    ServerContext.SetHttpContextAccessor(httpContextAccessor);
    ServerContext.Instance.InitializeHttpClient(new APP.Framework.Communication.HttpClientIdentityProvider(httpContextAccessor));
    ServerContext.Instance.InitializeWindowsClient(new WindowClientIdentityProvider());

    // Bridge ConfigurationManager.AppSettings → IConfiguration for APP.BL / APP.Framework on .NET 10
    AppConfig.SetConfiguration(app.Configuration);

    // LLBLGen 5.x — register SqlClient DbProviderFactory and master connection string.
    // On .NET Core the provider factory is no longer auto-discovered from machine.config;
    // it must be registered explicitly, otherwise ObtainDbProviderFactoryInfo throws.
    RuntimeConfiguration.AddConnectionString(
        APP.LBL.DatabaseSpecific.DataAccessAdapter.ConnectionStringKeyName,
        masterConnStr);
    RuntimeConfiguration.ConfigureDQE<SD.LLBLGen.Pro.DQE.SqlServer.SQLServerDQEConfiguration>(
        c => c.AddDbProviderFactory(typeof(System.Data.SqlClient.SqlClientFactory), "System.Data.SqlClient"));

    // LLBLGen 5.x — mark saved entities as fetched (required for change tracking)
    EntityBase2.MarkSavedEntitiesAsFetched = true;

    // GemBox licenses — read from config (moved from hardcoded Global.asax values)
    GemBox.Document.ComponentInfo.SetLicense(app.Configuration["GemBox:DocumentLicense"] ?? "FREE-LIMITED-KEY");
    GemBox.Spreadsheet.SpreadsheetInfo.SetLicense(app.Configuration["GemBox:SpreadsheetLicense"] ?? "FREE-LIMITED-KEY");
    GemBox.Pdf.ComponentInfo.SetLicense(app.Configuration["GemBox:PdfLicense"] ?? "FREE-LIMITED-KEY");

    // ── Middleware pipeline ───────────────────────────────────────────────────
    // HSTS — only in production (not dev where self-signed certs are common)
    if (!app.Environment.IsDevelopment())
        app.UseHsts();
    else
        app.UseHttpLogging();

    if (!string.IsNullOrWhiteSpace(allowedOrigins))
        app.UseCors("AppAiCors");

    // OPTIONS pre-flight response (replaces Application_EndRequest)
    app.Use(async (context, next) =>
    {
        if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 200;
            return;
        }
        await next();
    });

    app.UseSession();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Route registration ────────────────────────────────────────────────────
    app.MapControllers();
    app.MapHub<AppBuilderAgentHub>("/signalr/apphub");

    // Health endpoint — /health returns JSON with db probe status
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            });
            await context.Response.WriteAsync(result);
        }
    }).AllowAnonymous();

    // Minimal API endpoints replacing .ashx / .aspx handlers
    LegacyEndpoints.Map(app);

    // React SPA fallback (build output in wwwroot/)
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "AppAI.Web failed to start");
    throw;
}
finally
{
    LogManager.Shutdown();
}
