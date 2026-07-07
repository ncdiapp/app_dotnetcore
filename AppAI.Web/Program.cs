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
#if MCP_GATEWAY
// McpGateway plugin
using McpGateway.Models;
using McpGateway.Services;
using McpGateway.Services.LlmProviders;
using McpGateway.Services.EmbeddingProviders;
using McpGateway.MCP.Tools;
using McpGateway.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi.Models;
using Polly;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
#endif

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

    // ── Print engine ─────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<AppAI.Web.Services.PrintTokenService>();

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

    builder.Services.AddScoped<PlmUnitContext>();
    builder.Services.AddScoped<PlmUnitConversionFilter>();

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

    // ── McpGateway Plugin ─────────────────────────────────────────────────────
#if MCP_GATEWAY

    // Config bindings
    builder.Services.Configure<MultiSourceApiSettings>(
        builder.Configuration.GetSection(MultiSourceApiSettings.SectionName));
    builder.Services.Configure<LlmEnrichmentSettings>(
        builder.Configuration.GetSection(LlmEnrichmentSettings.SectionName));
    builder.Services.Configure<SemanticSearchSettings>(
        builder.Configuration.GetSection(SemanticSearchSettings.SectionName));
    builder.Services.Configure<DataAnalysisSettings>(
        builder.Configuration.GetSection(DataAnalysisSettings.SectionName));

    // LLM provider for endpoint enrichment
    var mcpLlmProvider = builder.Configuration["LlmEnrichment:Provider"] ?? "Anthropic";
    switch (mcpLlmProvider)
    {
        case "OpenAI":
            builder.Services.AddSingleton<ILlmProvider, OpenAiProvider>();
            break;
        case "AzureOpenAI":
            builder.Services.AddSingleton<ILlmProvider, AzureOpenAiProvider>();
            break;
        case "Gemini":
            builder.Services.AddSingleton<ILlmProvider, GeminiProvider>();
            break;
        default: // "Anthropic"
            builder.Services.AddSingleton<ILlmProvider, AnthropicProvider>();
            break;
    }

    // Embedding provider for RAG semantic search
    var mcpEmbeddingProvider = builder.Configuration["SemanticSearch:Provider"] ?? "None";
    switch (mcpEmbeddingProvider)
    {
        case "LocalOnnx":
            builder.Services.AddSingleton<IEmbeddingProvider, LocalOnnxEmbeddingProvider>();
            break;
        case "AzureOpenAI":
            builder.Services.AddSingleton<IEmbeddingProvider, AzureOpenAiEmbeddingProvider>();
            break;
        case "OpenAI":
            builder.Services.AddSingleton<IEmbeddingProvider, OpenAiEmbeddingProvider>();
            break;
        default: // "None"
            builder.Services.AddSingleton<IEmbeddingProvider, NoOpEmbeddingProvider>();
            break;
    }

    builder.Services.AddSingleton<ISemanticSearchService, SemanticSearchService>();
    builder.Services.AddSingleton<IRuntimeConfigService, RuntimeConfigService>();
    builder.Services.AddSingleton<ILlmEnrichmentService, LlmEnrichmentService>();
    builder.Services.AddSingleton<ISwaggerService, SwaggerService>();

    // TokenStore: singleton registered under both interface and as IHostedService
    builder.Services.AddSingleton<TokenStore>();
    builder.Services.AddSingleton<ITokenStore>(sp => sp.GetRequiredService<TokenStore>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<TokenStore>());

    // Named HttpClients per ApiSource with Polly resilience
    var mcpSources = builder.Configuration
        .GetSection(MultiSourceApiSettings.SectionName)
        .Get<MultiSourceApiSettings>()?.Sources ?? [];
    foreach (var src in mcpSources)
    {
        var timeoutSeconds = src.TimeoutSeconds > 0 ? src.TimeoutSeconds : 30;
        builder.Services.AddHttpClient(src.Name)
            .AddResilienceHandler($"{src.Name}-pipeline", pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(timeoutSeconds));
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay             = TimeSpan.FromMilliseconds(300),
                    BackoffType       = DelayBackoffType.Exponential,
                    UseJitter         = true,
                    ShouldHandle      = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError)
                });
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration  = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    FailureRatio      = 0.5,
                    BreakDuration     = TimeSpan.FromSeconds(30)
                });
            });
    }

    builder.Services.AddSingleton<IApiClient, ApiClient>();

    // Audit: real service only when AuditDb connection string is provided
    var auditConnStr = builder.Configuration.GetConnectionString("AuditDb");
    if (!string.IsNullOrWhiteSpace(auditConnStr))
        builder.Services.AddSingleton<IAuditService, AuditService>();
    else
        builder.Services.AddSingleton<IAuditService, McpNullAuditService>();

    builder.Services.AddSingleton<IDataAnalysisCacheService, DataAnalysisCacheService>();
    builder.Services.AddSingleton<IAnalysisService, AnalysisService>();
    builder.Services.AddHostedService<DataAnalysisStartupService>();

    builder.Services.AddAntiforgery();

    // Rate limiter — scoped to /mcp* and /auth* paths only (never affects /webapi/* routes)
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            var path = ctx.Request.Path.Value ?? "";
            if (!path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
                return RateLimitPartition.GetNoLimiter("bypass");

            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = 120,
                Window               = TimeSpan.FromMinutes(1),
                SegmentsPerWindow    = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            });
        });
        options.AddPolicy("auth", ctx =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 10,
                Window               = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            });
        });
    });

    // Swagger UI for McpGateway management endpoints (dev only)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "AppAI MCP Gateway API",
            Version     = "v1",
            Description = "MCP Gateway exposing multi-source API endpoints as MCP tools. Connect at /appai/mcp."
        });
    });

    // MCP Server — scan McpGateway's assembly for [McpServerToolType] classes
    var mcpStateless = builder.Configuration.GetValue<bool>("ApiSources:StatelessMode", false);
    builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "AppAI MCP Gateway", Version = "1.0.0" };
    })
    .WithHttpTransport(options =>
    {
        options.Stateless = mcpStateless;
        options.IdleTimeout = TimeSpan.FromHours(2);
    })
    .WithToolsFromAssembly(typeof(SwaggerTools).Assembly);
#endif

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // Redirect bare root → /appai/health so the host status is visible at localhost:<port>/
    app.MapGet("/", () => Results.Redirect("/appai/health")).ExcludeFromDescription();

    // Match the /appai path prefix used by the old IIS deployment and React proxy.
    // UseRouting MUST be called explicitly here — if omitted, ASP.NET Core auto-inserts it
    // before UsePathBase, so the router sees "/appai/health" instead of "/health".
    app.UsePathBase("/appai");
    app.UseRouting();

    // Correlation ID propagation (populates NLog MDLC for every request)
#if MCP_GATEWAY
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseRateLimiter();
#endif

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

    // ── McpGateway routes ─────────────────────────────────────────────────────
#if MCP_GATEWAY

    // Optional API Key guard for /mcp* and /auth* paths
    var mcpApiKey = builder.Configuration["Auth:ApiKey"];
    if (!string.IsNullOrEmpty(mcpApiKey))
    {
        app.Use(async (ctx, next) =>
        {
            var path = ctx.Request.Path.Value ?? "";
            bool guarded = path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase)
                        || path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase);
            if (!guarded) { await next(); return; }

            var provided = ctx.Request.Headers.TryGetValue("X-Api-Key", out var k)
                ? Encoding.UTF8.GetBytes(k.ToString()) : Array.Empty<byte>();
            var configured = Encoding.UTF8.GetBytes(mcpApiKey);

            if (!CryptographicOperations.FixedTimeEquals(provided, configured))
            {
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"X-Api-Key required\"}");
                return;
            }
            await next();
        });
    }

    // Swagger UI (dev only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AppAI MCP Gateway v1"));
    }

    // MCP endpoints — accessible at http://localhost:52740/appai/mcp
    app.MapMcp("/mcp");
#endif

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
