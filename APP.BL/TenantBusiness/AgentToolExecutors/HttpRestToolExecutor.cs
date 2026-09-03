using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.TenantBusiness.AgentToolExecutors
{
    /// <summary>
    /// Calls an external HTTP REST endpoint.
    /// ToolConfig: {"Url":"https://api.example.com/{param}","Method":"GET","Headers":{"Authorization":"Bearer token"},"BodyTemplate":""}
    /// {argName} placeholders in Url and BodyTemplate are replaced from args.
    /// </summary>
    public static class HttpRestToolExecutor
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        public static async Task<string> ExecuteAsync(
            string                             toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct)
        {
            var cfg = ParseConfig(toolConfig);
            if (string.IsNullOrWhiteSpace(cfg.Url))
                return JsonConvert.SerializeObject(new { Error = "HttpRest ToolConfig requires Url." });

            var resolvedUrl = ReplacePlaceholders(cfg.Url, args);
            var method = new HttpMethod((cfg.Method ?? "GET").ToUpperInvariant());
            var request = new HttpRequestMessage(method, resolvedUrl);

            foreach (var header in cfg.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (!string.IsNullOrWhiteSpace(cfg.BodyTemplate) && method != HttpMethod.Get && method != HttpMethod.Delete)
            {
                var body = ReplacePlaceholders(cfg.BodyTemplate, args);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return JsonConvert.SerializeObject(new { Error = $"HTTP {(int)response.StatusCode}", Body = content });

                return content;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }

        private static string ReplacePlaceholders(string template, IReadOnlyDictionary<string, string> args)
        {
            if (string.IsNullOrEmpty(template) || args == null) return template;
            return Regex.Replace(template, @"\{(\w+)\}", m =>
            {
                var key = m.Groups[1].Value;
                return args.TryGetValue(key, out var val) ? Uri.EscapeDataString(val ?? "") : m.Value;
            });
        }

        private static (string Url, string Method, Dictionary<string, string> Headers, string BodyTemplate) ParseConfig(string toolConfig)
        {
            try
            {
                var obj = JObject.Parse(toolConfig ?? "{}");
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var headersToken = obj["Headers"] as JObject;
                if (headersToken != null)
                    foreach (var prop in headersToken.Properties())
                        headers[prop.Name] = prop.Value?.ToString() ?? "";
                return (
                    Url:          obj["Url"]?.ToString() ?? "",
                    Method:       obj["Method"]?.ToString() ?? "GET",
                    Headers:      headers,
                    BodyTemplate: obj["BodyTemplate"]?.ToString() ?? "");
            }
            catch { return ("", "GET", new Dictionary<string, string>(), ""); }
        }
    }
}
