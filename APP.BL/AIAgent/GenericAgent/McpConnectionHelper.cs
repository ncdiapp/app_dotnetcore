using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using App.BL.TenantBusiness;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace App.BL.AIAgent.GenericAgent
{
    public sealed record McpTestResult(bool Success, string Message, int ToolCount, List<string> ToolNames);

    public static class McpConnectionHelper
    {
        // Redirects are not followed: a registered URL must not be able to bounce the server to another (blocked) host.
        public static HttpClient CreateHttpClient() =>
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };

        /// <summary>Throws when the registration breaks <see cref="McpSecurityPolicy"/> (used before every connect).</summary>
        public static void EnsureAllowed(AppAgentMcpServerDto server)
        {
            var error = McpSecurityPolicy.Validate(server);
            if (error != null) throw new InvalidOperationException(error);
        }

        /// <summary>
        /// Builds request headers for a streamable-http MCP server:
        /// static Headers (stored encrypted), then HeadersFromEnv (header -> env var name), then Bearer env var as Authorization.
        /// Only environment variables named MCP_* are readable.
        /// </summary>
        public static Dictionary<string, string> BuildHeaders(AppAgentMcpServerDto server, List<string> warnings = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in McpHeaderSecrets.Parse(McpHeaderSecrets.Unprotect(server.Headers)))
                headers[kv.Key] = kv.Value;

            foreach (var kv in McpHeaderSecrets.Parse(server.HeadersFromEnv))
            {
                var value = ReadEnv(kv.Value, $"header '{kv.Key}'", server, warnings);
                if (!string.IsNullOrEmpty(value)) headers[kv.Key] = value;
            }

            if (!string.IsNullOrWhiteSpace(server.BearerTokenEnvVar))
            {
                var token = ReadEnv(server.BearerTokenEnvVar, "bearer token", server, warnings);
                if (!string.IsNullOrEmpty(token)) headers["Authorization"] = "Bearer " + token;
            }

            return headers;
        }

        // ── calling an MCP server as the signed-in user ────────────────────────────────────────────────────────────
        // A server registration holds ONE set of static headers for everybody. For the in-house gateway that would run
        // every user's agent with the same identity. Servers listed here instead get a short-lived token minted for the
        // user who started the run. Both are set once by the host (Program.cs) from the OAuth settings.

        /// <summary>Exact MCP server URLs that must be called with the signed-in user's own token. Empty = none.</summary>
        public static ISet<string> UserTokenServerUrls { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Mints a token that makes the gateway act as (userId, companyId); set by the host. Null result = cannot.</summary>
        public static Func<int, int, string> UserTokenFactory { get; set; }

        public static bool RequiresUserToken(string serverUrl) =>
            !string.IsNullOrWhiteSpace(serverUrl) && UserTokenServerUrls.Contains(serverUrl.Trim());

        /// <summary>
        /// The user token for this run, or throws. It never falls back to the server's stored headers: a run without a
        /// signed-in user must not borrow a shared identity (unattended jobs use a server registration without this setting).
        /// </summary>
        public static string GetUserToken(AppAgentMcpServerDto server, int userId, int companyId)
        {
            if (userId <= 0 || companyId <= 0)
                throw new InvalidOperationException($"MCP server '{server.ServerName}' acts as the signed-in user, but this run has no signed-in user.");
            var token = UserTokenFactory?.Invoke(userId, companyId);
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException($"MCP server '{server.ServerName}' acts as the signed-in user, but no user token could be issued.");
            return token;
        }

        public static HttpClientTransportOptions BuildTransportOptions(AppAgentMcpServerDto server, List<string> warnings = null, string userToken = null)
        {
            var headers = BuildHeaders(server, warnings);
            if (userToken != null)
            {
                // Only the user's token identifies the caller: drop every stored credential so none can take precedence.
                foreach (var credential in new[] { "Authorization", "IntergrationAccessToken", "CurrentUserSessionId", "X-Api-Key" })
                    headers.Remove(credential);
                headers["Authorization"] = "Bearer " + userToken;
            }

            return new HttpClientTransportOptions
            {
                Endpoint          = new Uri(server.ServerUrl),
                TransportMode     = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = headers
            };
        }

        public static async Task<McpTestResult> TestConnectionAsync(AppAgentMcpServerDto server, CancellationToken ct)
        {
            var policyError = McpSecurityPolicy.Validate(server);
            if (policyError != null)
                return new McpTestResult(false, policyError, 0, new List<string>());
            if (!string.Equals(server.ServerType, "streamable-http", StringComparison.OrdinalIgnoreCase))
                return new McpTestResult(false, "Test connection supports streamable-http servers only.", 0, new List<string>());

            var warnings = new List<string>();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            try
            {
                var options   = BuildTransportOptions(server with { ServerUrl = server.ServerUrl.Trim() }, warnings);
                using var http = CreateHttpClient();
                var transport = new HttpClientTransport(options, http, NullLoggerFactory.Instance, ownsHttpClient: false);
                await using var client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token).ConfigureAwait(false);
                var tools = await client.ListToolsAsync(cancellationToken: cts.Token).ConfigureAwait(false);
                var names = tools.Select(t => t.Name).ToList();
                var msg   = $"Connected. {names.Count} tool(s) found.";
                if (warnings.Count > 0) msg += " Warning: " + string.Join(" ", warnings);
                return new McpTestResult(true, msg, names.Count, names);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return new McpTestResult(false, WithWarnings("Timed out after 15 seconds.", warnings), 0, new List<string>());
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "MCP test connection failed for {0}", server.ServerUrl);
                return new McpTestResult(false, WithWarnings(ex.Message, warnings), 0, new List<string>());
            }
        }

        /// <summary>
        /// Connects to the server and returns its tools as catalog rows (description, parameter names, risk hint).
        /// Risk comes from the server's read-only/destructive annotations when sent, otherwise from the tool name.
        /// </summary>
        public static async Task<(McpTestResult Result, List<AppAgentToolCatalogDto> Tools)> ListCatalogToolsAsync(
            AppAgentMcpServerDto server, CancellationToken ct)
        {
            var none = new List<AppAgentToolCatalogDto>();
            var policyError = McpSecurityPolicy.Validate(server);
            if (policyError != null)
                return (new McpTestResult(false, policyError, 0, new List<string>()), none);
            if (!string.Equals(server.ServerType, "streamable-http", StringComparison.OrdinalIgnoreCase))
                return (new McpTestResult(false, "Sync supports streamable-http servers only.", 0, new List<string>()), none);

            var warnings = new List<string>();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                var options = BuildTransportOptions(server with { ServerUrl = server.ServerUrl.Trim() }, warnings);
                using var http = CreateHttpClient();
                var transport = new HttpClientTransport(options, http, NullLoggerFactory.Instance, ownsHttpClient: false);
                await using var client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token).ConfigureAwait(false);
                var tools = await client.ListToolsAsync(cancellationToken: cts.Token).ConfigureAwait(false);

                var rows = tools.Select(t =>
                {
                    var annotations = t.ProtocolTool?.Annotations;
                    return new AppAgentToolCatalogDto(
                        Source:       "mcp",
                        LibraryKey:   server.SkillKey,
                        ToolName:     t.Name,
                        Description:  t.Description ?? "",
                        InputSummary: ToolRiskGuesser.SummarizeSchema(t.JsonSchema.ToString()),
                        Risk:         ToolRiskGuesser.Guess(t.Name, annotations?.ReadOnlyHint, annotations?.DestructiveHint),
                        McpServerId:  server.McpServerId);
                }).ToList();

                var msg = $"Synced {rows.Count} tool(s).";
                if (warnings.Count > 0) msg += " Warning: " + string.Join(" ", warnings);
                return (new McpTestResult(true, msg, rows.Count, rows.Select(r => r.ToolName).ToList()), rows);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return (new McpTestResult(false, WithWarnings("Timed out after 30 seconds.", warnings), 0, new List<string>()), none);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "MCP tool sync failed for {0}", server.ServerUrl);
                return (new McpTestResult(false, WithWarnings(ex.Message, warnings), 0, new List<string>()), none);
            }
        }

        private static string WithWarnings(string message, List<string> warnings) =>
            warnings.Count == 0 ? message : message + " " + string.Join(" ", warnings);

        // Reads an environment variable for a header. Only MCP_* names are allowed so a registration cannot be used to
        // read unrelated server secrets. Names (never values) are logged.
        private static string ReadEnv(string name, string purpose, AppAgentMcpServerDto server, List<string> warnings)
        {
            name = name?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            var log = NLog.LogManager.GetCurrentClassLogger();
            if (!McpSecurityPolicy.IsAllowedEnvVarName(name))
            {
                log.Warn("MCP server {0} asked for env var '{1}' ({2}) — blocked, names must start with {3}",
                    server.ServerName, name, purpose, McpSecurityPolicy.EnvVarPrefix);
                warnings?.Add($"Environment variable '{name}' ({purpose}) is not allowed — names must start with {McpSecurityPolicy.EnvVarPrefix}.");
                return null;
            }

            var value = Environment.GetEnvironmentVariable(name)
                ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            log.Info("MCP server {0}: env var '{1}' ({2}) {3}", server.ServerName, name, purpose, string.IsNullOrEmpty(value) ? "is not set" : "resolved");
            if (string.IsNullOrEmpty(value))
                warnings?.Add($"Environment variable '{name}' ({purpose}) is not set.");
            return value;
        }
    }
}
