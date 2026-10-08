using System.Net;
using System.Threading.RateLimiting;
using AppAI.Web.Auth;
using AppAI.Web.Services;
using McpGateway.MCP.Tools;
using McpGateway.Middleware;
using McpGateway.Models;
using McpGateway.Services;
using McpGateway.Services.EmbeddingProviders;
using McpGateway.Services.LlmProviders;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace AppAI.Web.Mcp;

/// <summary>
/// Wires the MCP gateway (Swagger -> MCP tools for Claude Desktop / ChatGPT Desktop ...) into the host.
/// Always on: Program.cs registers it unconditionally.
/// </summary>
public static class McpGatewayExtensions
{
    public static IServiceCollection AddMcpGateway(this IServiceCollection services, IConfiguration configuration)
    {
        // Config bindings
        services.Configure<MultiSourceApiSettings>(configuration.GetSection(MultiSourceApiSettings.SectionName));
        services.Configure<LlmEnrichmentSettings>(configuration.GetSection(LlmEnrichmentSettings.SectionName));
        services.Configure<SemanticSearchSettings>(configuration.GetSection(SemanticSearchSettings.SectionName));
        services.Configure<DataAnalysisSettings>(configuration.GetSection(DataAnalysisSettings.SectionName));

        // LLM provider for endpoint enrichment
        switch (configuration["LlmEnrichment:Provider"] ?? "Anthropic")
        {
            case "OpenAI":      services.AddSingleton<ILlmProvider, OpenAiProvider>(); break;
            case "AzureOpenAI": services.AddSingleton<ILlmProvider, AzureOpenAiProvider>(); break;
            case "Gemini":      services.AddSingleton<ILlmProvider, GeminiProvider>(); break;
            default:            services.AddSingleton<ILlmProvider, AnthropicProvider>(); break;
        }

        // Embedding provider for RAG semantic search
        switch (configuration["SemanticSearch:Provider"] ?? "None")
        {
            case "LocalOnnx":   services.AddSingleton<IEmbeddingProvider, LocalOnnxEmbeddingProvider>(); break;
            case "AzureOpenAI": services.AddSingleton<IEmbeddingProvider, AzureOpenAiEmbeddingProvider>(); break;
            case "OpenAI":      services.AddSingleton<IEmbeddingProvider, OpenAiEmbeddingProvider>(); break;
            default:            services.AddSingleton<IEmbeddingProvider, NoOpEmbeddingProvider>(); break;
        }

        services.AddSingleton<ISemanticSearchService, SemanticSearchService>();
        services.AddSingleton<IRuntimeConfigService, RuntimeConfigService>();
        services.AddSingleton<ILlmEnrichmentService, LlmEnrichmentService>();
        services.AddSingleton<ISwaggerService, SwaggerService>();

        // Named HttpClients per ApiSource with Polly resilience
        var sources = configuration.GetSection(MultiSourceApiSettings.SectionName).Get<MultiSourceApiSettings>()?.Sources ?? [];
        foreach (var src in sources)
        {
            var timeoutSeconds = src.TimeoutSeconds > 0 ? src.TimeoutSeconds : 30;
            services.AddHttpClient(src.Name)
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

        services.AddSingleton<IApiClient, ApiClient>();

        // Audit: queued, per-tenant (dbo.AppMcpAuditLog, V046). One instance serves IAuditService and the background writer.
        services.AddSingleton<IAuditSink, McpTenantAuditSink>();
        services.AddSingleton<QueuedAuditService>();
        services.AddSingleton<IAuditService>(sp => sp.GetRequiredService<QueuedAuditService>());
        services.AddHostedService(sp => sp.GetRequiredService<QueuedAuditService>());

        services.AddSingleton<IDataAnalysisCacheService, DataAnalysisCacheService>();
        services.AddSingleton<IAnalysisService, AnalysisService>();
        services.AddSingleton<IMcpCallerContext, McpCallerContext>();

        // Which APIs an external user may see/call: security-group grants in the tenant DB (V047), cached briefly.
        services.AddSingleton<IApiAccessProvider, McpApiAccessProvider>();
        services.AddSingleton<IApiAccessPolicy, ApiAccessPolicy>();

        // Rate limiter — scoped to /mcp* paths only (never affects /webapi/* routes)
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var path = ctx.Request.Path.Value ?? "";
                if (!path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase))
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
        });

        // OpenAPI document of AppAI's own controllers: the gateway indexes it as the "AppAI" source (see McpSwaggerSetup).
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(McpSwaggerSetup.Configure);

        // MCP Server — scan McpGateway's assembly for [McpServerToolType] classes
        services.AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = "AppAI MCP Gateway", Version = "1.0.0" };
            })
            .WithHttpTransport(options =>
            {
                options.Stateless = configuration.GetValue("ApiSources:StatelessMode", false);
                options.IdleTimeout = TimeSpan.FromHours(2);
            })
            .WithToolsFromAssembly(typeof(SwaggerTools).Assembly);

        return services;
    }

    /// <summary>Early in the pipeline: correlation id for logs, and the /mcp rate limiter.</summary>
    public static void UseMcpGatewayEarly(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseRateLimiter();
    }

    /// <summary>After authentication: token check on /mcp, the loopback-only API spec, and the MCP endpoint.</summary>
    public static void UseMcpGateway(this WebApplication app)
    {
        // Every /mcp request must carry a valid IntergrationAccessToken (per-user Integration token).
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/mcp"),
            branch => branch.UseMiddleware<IntegrationTokenMiddleware>());

        // The OpenAPI document lists every endpoint, so only the gateway itself (loopback) may fetch it; anyone else
        // gets a plain 404. Swagger UI is for local development only.
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/swagger"),
            branch => branch.Use(async (ctx, next) =>
            {
                var remote = ctx.Connection.RemoteIpAddress;
                if (remote != null && IPAddress.IsLoopback(remote)) { await next(); return; }
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            }));
        app.UseSwagger();
        if (app.Environment.IsDevelopment())
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/appai/swagger/v1/swagger.json", "AppAI API v1"));

        // MCP endpoints — accessible at http://localhost:52740/appai/mcp
        app.MapMcp("/mcp");
    }
}
