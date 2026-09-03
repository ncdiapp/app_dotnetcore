using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AppDataIntegrationAgent
{
    public static class CursorCloudClient
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(AppDataIntegrationAgentConfig.HttpClientTimeoutMinutes) };
            return client;
        }

        public class CreateResult
        {
            public string AgentId { get; set; }
            public string RunId { get; set; }
            public string Raw { get; set; }
        }

        public static async Task<CreateResult> CreateAgentAsync(
            string promptText,
            object mcpServers,
            CancellationToken ct)
        {
            var body = new JObject
            {
                ["prompt"] = new JObject { ["text"] = promptText ?? "" },
                ["model"] = new JObject { ["id"] = AppDataIntegrationAgentConfig.ModelId },
                ["autoCreatePR"] = AppDataIntegrationAgentConfig.AutoCreatePr,
                ["mode"] = "agent"
            };

            if (AppDataIntegrationAgentConfig.AttachRepo && !string.IsNullOrWhiteSpace(AppDataIntegrationAgentConfig.RepoUrl))
            {
                body["repos"] = new JArray
                {
                    new JObject
                    {
                        ["url"] = AppDataIntegrationAgentConfig.RepoUrl,
                        ["startingRef"] = AppDataIntegrationAgentConfig.RepoRef
                    }
                };
                body["workOnCurrentBranch"] = false;
            }

            if (mcpServers != null)
                body["mcpServers"] = JToken.FromObject(mcpServers);

            var json = await SendAsync(HttpMethod.Post, "/v1/agents", body, ct).ConfigureAwait(false);
            var parsed = JObject.Parse(json);
            return new CreateResult
            {
                AgentId = (string)(parsed["agent"]?["id"] ?? parsed["id"]),
                RunId = (string)(parsed["run"]?["id"] ?? parsed["agent"]?["latestRunId"] ?? parsed["latestRunId"]),
                Raw = json
            };
        }

        public static async Task<CreateResult> FollowUpAsync(
            string agentId,
            string promptText,
            object mcpServers,
            CancellationToken ct)
        {
            var body = new JObject
            {
                ["prompt"] = new JObject { ["text"] = promptText ?? "" }
            };
            if (mcpServers != null)
                body["mcpServers"] = JToken.FromObject(mcpServers);

            var json = await SendAsync(HttpMethod.Post, "/v1/agents/" + agentId + "/runs", body, ct).ConfigureAwait(false);
            var parsed = JObject.Parse(json);
            return new CreateResult
            {
                AgentId = agentId,
                RunId = ParseRunId(parsed),
                Raw = json
            };
        }

        private static string ParseRunId(JObject parsed)
        {
            if (parsed == null) return null;
            var id = (string)(parsed["run"]?["id"]
                ?? parsed["runId"]
                ?? parsed["latestRunId"]
                ?? parsed["id"]);
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (id.StartsWith("bc-", StringComparison.OrdinalIgnoreCase))
                return (string)(parsed["run"]?["id"] ?? parsed["latestRunId"] ?? parsed["runId"]);
            return id;
        }

        public static async Task CancelAsync(string agentId, string runId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(runId)) return;
            try
            {
                await SendAsync(HttpMethod.Post, "/v1/agents/" + agentId + "/runs/" + runId + "/cancel", null, ct)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
        }

        public static async Task<JObject> GetRunAsync(string agentId, string runId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(runId)) return null;
            try
            {
                var json = await SendAsync(HttpMethod.Get, "/v1/agents/" + agentId + "/runs/" + runId, null, ct)
                    .ConfigureAwait(false);
                return JObject.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<List<JObject>> ListRunsAsync(string agentId, CancellationToken ct)
        {
            var list = new List<JObject>();
            if (string.IsNullOrWhiteSpace(agentId)) return list;
            try
            {
                var json = await SendAsync(HttpMethod.Get, "/v1/agents/" + agentId + "/runs?limit=10", null, ct)
                    .ConfigureAwait(false);
                var parsed = JObject.Parse(json);
                var items = parsed["items"] as JArray;
                if (items == null) return list;
                foreach (var item in items)
                {
                    var obj = item as JObject;
                    if (obj != null) list.Add(obj);
                }
            }
            catch
            {
            }
            return list;
        }

        public static async Task EnsureIdleAsync(string agentId, string latestRunId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(agentId)) return;
            _ = latestRunId;
            var runs = await ListRunsAsync(agentId, ct).ConfigureAwait(false);
            foreach (var run in runs)
            {
                var status = ((string)run["status"] ?? "").ToUpperInvariant();
                var id = (string)run["id"];
                if (IsActiveStatus(status))
                    await CancelAsync(agentId, id, ct).ConfigureAwait(false);
            }

            for (var i = 0; i < 20 && !ct.IsCancellationRequested; i++)
            {
                runs = await ListRunsAsync(agentId, ct).ConfigureAwait(false);
                var busy = runs.Any(r => IsActiveStatus(((string)r["status"] ?? "").ToUpperInvariant()));
                if (!busy) return;
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
        }

        public static bool IsActiveStatus(string status)
        {
            return string.Equals(status, "CREATING", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "RUNNING", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "QUEUED", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBusyError(Exception ex)
        {
            var msg = ex?.Message ?? "";
            return msg.IndexOf("agent_busy", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("already has an active run", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsArchivedError(Exception ex)
        {
            return (ex?.Message ?? "").IndexOf("agent_archived", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task UnarchiveAgentAsync(string agentId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(agentId)) return;
            await SendAsync(HttpMethod.Post, "/v1/agents/" + agentId + "/unarchive", new JObject(), ct)
                .ConfigureAwait(false);
        }

        public static bool IsStreamGone(Exception ex)
        {
            return IsRecoverableStreamMessage(ex?.Message);
        }

        public static bool IsRecoverableStreamMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            var msg = message;
            return msg.IndexOf("no longer available", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("stream_expired", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("stream expired", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task StreamRunAsync(
            string agentId,
            string runId,
            Action<string, JObject> onEvent,
            CancellationToken ct)
        {
            var key = AppDataIntegrationAgentConfig.ApiKey;
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Cursor:ApiKey is not configured.");

            var url = AppDataIntegrationAgentConfig.ApiBaseUrl + "/v1/agents/" + agentId + "/runs/" + runId + "/stream";
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplyAuth(req, key);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                using (var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new InvalidOperationException("Cursor stream failed (" + (int)resp.StatusCode + "): " + Trunc(raw, 2000));
                    }

                    using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var reader = new StreamReader(stream))
                    {
                        string eventName = null;
                        var data = new StringBuilder();
                        while (!reader.EndOfStream && !ct.IsCancellationRequested)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);
                            if (line == null) break;
                            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                            {
                                eventName = line.Substring(6).Trim();
                                continue;
                            }
                            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                            {
                                if (data.Length > 0) data.Append('\n');
                                data.Append(line.Substring(5).TrimStart());
                                continue;
                            }
                            if (line.Length == 0)
                            {
                                if (DispatchSse(eventName, data.ToString(), onEvent))
                                    return;
                                eventName = null;
                                data.Clear();
                            }
                        }
                        DispatchSse(eventName, data.ToString(), onEvent);
                    }
                }
            }
        }

        private static bool DispatchSse(string eventName, string data, Action<string, JObject> onEvent)
        {
            if (string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(data) || onEvent == null)
                return false;
            JObject payload;
            try { payload = JObject.Parse(data); }
            catch { payload = new JObject { ["raw"] = data }; }
            onEvent(eventName, payload);
            return string.Equals(eventName, "done", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventName, "error", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<string> SendAsync(HttpMethod method, string path, JObject body, CancellationToken ct)
        {
            var key = AppDataIntegrationAgentConfig.ApiKey;
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Cursor:ApiKey is not configured. Set Cursor:ApiKey in appsettings.");

            using (var req = new HttpRequestMessage(method, AppDataIntegrationAgentConfig.ApiBaseUrl + path))
            {
                ApplyAuth(req, key);
                if (body != null)
                    req.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var resp = await Http.SendAsync(req, ct).ConfigureAwait(false))
                {
                    var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                        throw new InvalidOperationException("Cursor API " + path + " failed (" + (int)resp.StatusCode + "): " + Trunc(text, 2000));
                    return text;
                }
            }
        }

        private static void ApplyAuth(HttpRequestMessage req, string key)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        private static string Trunc(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max) return value;
            return value.Substring(0, max);
        }

        public static async Task<List<string>> ListArtifactPathsAsync(string agentId, CancellationToken ct)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(agentId)) return list;
            try
            {
                var json = await SendAsync(HttpMethod.Get, "/v1/agents/" + agentId + "/artifacts", null, ct)
                    .ConfigureAwait(false);
                var parsed = JObject.Parse(json);
                var items = parsed["items"] as JArray ?? parsed["artifacts"] as JArray;
                if (items == null) return list;
                foreach (var item in items)
                {
                    var path = (string)(item["path"] ?? item["absolutePath"] ?? item["relativePath"]);
                    if (!string.IsNullOrWhiteSpace(path))
                        list.Add(path);
                }
            }
            catch
            {
            }
            return list;
        }

        public static async Task<byte[]> DownloadArtifactBytesAsync(string agentId, string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(path))
                return null;
            var query = "/v1/agents/" + agentId + "/artifacts/download?path=" + Uri.EscapeDataString(path);
            var json = await SendAsync(HttpMethod.Get, query, null, ct).ConfigureAwait(false);
            var url = (string)JObject.Parse(json)["url"];
            if (string.IsNullOrWhiteSpace(url)) return null;
            using (var resp = await Http.GetAsync(url, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }
    }
}
