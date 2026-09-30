using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using App.BL.TenantBusiness;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Limits what a tenant admin can make the server do through an MCP server registration:
    ///  - environment variables the server may read into headers (prefix MCP_ only), and
    ///  - which URLs the server may connect to (no link-local / cloud-metadata / unspecified / multicast targets).
    /// Loopback and private-network addresses stay allowed (PLM and ERP servers usually run there); a per-tenant
    /// host allow-list would be the next step.
    /// </summary>
    public static class McpSecurityPolicy
    {
        public const string EnvVarPrefix = "MCP_";

        private static readonly Regex EnvName = new Regex(@"^MCP_[A-Za-z0-9_]{1,100}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsAllowedEnvVarName(string name) => !string.IsNullOrWhiteSpace(name) && EnvName.IsMatch(name.Trim());

        /// <summary>Returns an error message, or null when the registration is acceptable.</summary>
        public static string Validate(AppAgentMcpServerDto server)
        {
            if (server == null) return "Server is required.";

            if (!string.IsNullOrWhiteSpace(server.BearerTokenEnvVar) && !IsAllowedEnvVarName(server.BearerTokenEnvVar))
                return $"Bearer token env var '{server.BearerTokenEnvVar.Trim()}' is not allowed — names must start with {EnvVarPrefix} (for example {EnvVarPrefix}BEARER_TOKEN).";

            if (!string.IsNullOrWhiteSpace(server.HeadersFromEnv))
            {
                System.Collections.Generic.Dictionary<string, string> map;
                try { map = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(server.HeadersFromEnv); }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug(ex, "HeadersFromEnv is not valid JSON");
                    return "Headers from env vars is not valid.";
                }
                var bad = map?.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v) && !IsAllowedEnvVarName(v));
                if (bad != null)
                    return $"Env var '{bad.Trim()}' is not allowed — names must start with {EnvVarPrefix}.";
            }

            if (string.Equals(server.ServerType, "streamable-http", StringComparison.OrdinalIgnoreCase) || string.Equals(server.ServerType, "sse", StringComparison.OrdinalIgnoreCase))
                return ValidateUrl(server.ServerUrl);

            return null;
        }

        public static string ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "Server URL is required.";
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return "Server URL is not a valid absolute URL.";
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return "Server URL must use http or https.";
            if (!string.IsNullOrEmpty(uri.UserInfo)) return "Server URL must not contain credentials (use headers instead).";

            IPAddress[] addresses;
            try { addresses = IPAddress.TryParse(uri.Host, out var literal) ? new[] { literal } : Dns.GetHostAddresses(uri.Host); }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Debug(ex, "MCP host could not be resolved: {0}", uri.Host);
                return $"Server host '{uri.Host}' could not be resolved.";
            }

            var blocked = addresses.FirstOrDefault(IsBlocked);
            return blocked == null ? null : $"Server URL points to a blocked address ({blocked}): link-local, cloud-metadata, unspecified and multicast addresses are not allowed.";
        }

        private static bool IsBlocked(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (IPAddress.Any.Equals(ip) || IPAddress.IPv6Any.Equals(ip) || IPAddress.Broadcast.Equals(ip)) return true;

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = ip.GetAddressBytes();
                return (b[0] == 169 && b[1] == 254) || b[0] >= 224;   // link-local incl. 169.254.169.254 metadata; multicast/reserved
            }
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                return ip.IsIPv6LinkLocal || ip.IsIPv6Multicast;
            return false;
        }
    }
}
