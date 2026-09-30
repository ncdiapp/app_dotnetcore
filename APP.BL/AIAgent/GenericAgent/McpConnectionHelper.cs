using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using App.BL.TenantBusiness;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent
{
    public sealed record McpTestResult(bool Success, string Message, int ToolCount, List<string> ToolNames);

    public static class McpConnectionHelper
    {
        /// <summary>
        /// Builds request headers for a streamable-http MCP server:
        /// static Headers, then HeadersFromEnv (header -> env var name), then Bearer env var as Authorization.
        /// </summary>
        public static Dictionary<string, string> BuildHeaders(AppAgentMcpServerDto server, List<string> warnings = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in ParseMap(server.Headers))
                headers[kv.Key] = kv.Value;

            foreach (var kv in ParseMap(server.HeadersFromEnv))
            {
                var value = ReadEnv(kv.Value);
                if (string.IsNullOrEmpty(value))
                    warnings?.Add($"Environment variable '{kv.Value}' (header '{kv.Key}') is not set.");
                else
                    headers[kv.Key] = value;
            }

            if (!string.IsNullOrWhiteSpace(server.BearerTokenEnvVar))
            {
                var token = ReadEnv(server.BearerTokenEnvVar.Trim());
                if (string.IsNullOrEmpty(token))
                    warnings?.Add($"Environment variable '{server.BearerTokenEnvVar.Trim()}' (bearer token) is not set.");
                else
                    headers["Authorization"] = "Bearer " + token;
            }

            return headers;
        }

        public static HttpClientTransportOptions BuildTransportOptions(AppAgentMcpServerDto server, List<string> warnings = null)
        {
            return new HttpClientTransportOptions
            {
                Endpoint          = new Uri(server.ServerUrl),
                TransportMode     = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = BuildHeaders(server, warnings)
            };
        }

        public static async Task<McpTestResult> TestConnectionAsync(AppAgentMcpServerDto server, CancellationToken ct)
        {
            if (server == null || string.IsNullOrWhiteSpace(server.ServerUrl))
                return new McpTestResult(false, "Server URL is required.", 0, new List<string>());
            if (!Uri.TryCreate(server.ServerUrl.Trim(), UriKind.Absolute, out _))
                return new McpTestResult(false, "Server URL is not a valid absolute URL.", 0, new List<string>());
            if (!string.Equals(server.ServerType, "streamable-http", StringComparison.OrdinalIgnoreCase))
                return new McpTestResult(false, "Test connection supports streamable-http servers only.", 0, new List<string>());

            var warnings = new List<string>();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            try
            {
                var options   = BuildTransportOptions(server with { ServerUrl = server.ServerUrl.Trim() }, warnings);
                using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
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
            if (server == null || string.IsNullOrWhiteSpace(server.ServerUrl) || !Uri.TryCreate(server.ServerUrl.Trim(), UriKind.Absolute, out _))
                return (new McpTestResult(false, "A valid Server URL is required.", 0, new List<string>()), none);
            if (!string.Equals(server.ServerType, "streamable-http", StringComparison.OrdinalIgnoreCase))
                return (new McpTestResult(false, "Sync supports streamable-http servers only.", 0, new List<string>()), none);

            var warnings = new List<string>();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                var options = BuildTransportOptions(server with { ServerUrl = server.ServerUrl.Trim() }, warnings);
                using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
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

        private static string ReadEnv(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return Environment.GetEnvironmentVariable(name.Trim())
                ?? Environment.GetEnvironmentVariable(name.Trim(), EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(name.Trim(), EnvironmentVariableTarget.Machine);
        }

        private static Dictionary<string, string> ParseMap(string json)
        {
            var map = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(json)) return map;
            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (parsed == null) return map;
                foreach (var kv in parsed)
                    if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null)
                        map[kv.Key.Trim()] = kv.Value;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Invalid MCP header JSON");
            }
            return map;
        }
    }
}
