using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.BL.CursorCloudAgent;
using App.BL.TenantBusiness;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// MCP server for Agent Management CursorCloudAgents runtime.
    /// Exposes AppAgentToolRegister (+ library subscriptions) for the session SkillKey.
    /// Auth: per-session MCP bearer token (same store as CursorCloudAgent).
    /// </summary>
    public static class AgentSkillMcpBL
    {
        public static object McpServerSpec(string mcpBaseUrl, string token)
        {
            if (string.IsNullOrWhiteSpace(mcpBaseUrl) || string.IsNullOrWhiteSpace(token))
                return null;
            return new[]
            {
                new
                {
                    name = "appai-agent-tools",
                    type = "http",
                    url = mcpBaseUrl.TrimEnd('/') + "/webapi/AgentSkillMcp/Invoke",
                    headers = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + token }
                    }
                }
            };
        }

        public static object HandleJsonRpc(JObject request)
        {
            var id = request["id"];
            var method = (string)request["method"];
            if (string.Equals(method, "notifications/initialized", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "notifications/cancelled", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                object result;
                if (string.Equals(method, "initialize", StringComparison.OrdinalIgnoreCase))
                    result = InitializeResult();
                else if (string.Equals(method, "ping", StringComparison.OrdinalIgnoreCase))
                    result = new { };
                else if (string.Equals(method, "tools/list", StringComparison.OrdinalIgnoreCase))
                    result = new { tools = ToolDescriptors(CursorCloudAgentContext.Current) };
                else if (string.Equals(method, "tools/call", StringComparison.OrdinalIgnoreCase))
                    result = CallToolSync(request["params"] as JObject);
                else
                    return RpcError(id, -32601, "Method not found: " + method);

                return new { jsonrpc = "2.0", id, result };
            }
            catch (Exception ex)
            {
                return RpcError(id, -32000, ex.Message);
            }
        }

        public static async Task<object> HandleJsonRpcAsync(JObject request, CancellationToken ct)
        {
            var method = (string)request["method"];
            if (string.Equals(method, "tools/call", StringComparison.OrdinalIgnoreCase))
            {
                var id = request["id"];
                try
                {
                    var result = await CallToolAsync(request["params"] as JObject, ct).ConfigureAwait(false);
                    return new { jsonrpc = "2.0", id, result };
                }
                catch (Exception ex)
                {
                    return RpcError(id, -32000, ex.Message);
                }
            }
            return HandleJsonRpc(request);
        }

        private static object CallToolSync(JObject p)
            => CallToolAsync(p, CancellationToken.None).GetAwaiter().GetResult();

        private static async Task<object> CallToolAsync(JObject p, CancellationToken ct)
        {
            var name = ((string)p?["name"] ?? "").Trim();
            var args = p?["arguments"] as JObject ?? new JObject();
            var session = CursorCloudAgentContext.Current;
            if (session == null)
                return ToolText("No agent session is bound to this MCP token.", true);

            if (string.Equals(name, "get_session_context", StringComparison.OrdinalIgnoreCase))
            {
                return ToolText(JsonConvert.SerializeObject(new
                {
                    session.SessionId,
                    session.SkillKey,
                    session.SaasApplicationId,
                    session.DataSourceRegisterId,
                    CloudAgentId = session.CloudAgentId
                }));
            }

            var skillKey = session.SkillKey;
            if (string.IsNullOrWhiteSpace(skillKey))
                return ToolText("Session has no SkillKey; cannot resolve Agent Management tools.", true);

            var dsId = session.Identity?.DataSourceId
                ?? (session.CompanyId.HasValue ? 0 : 0);
            if (session.Identity.HasValue)
                dsId = session.Identity.Value.DataSourceId;

            var tools = dsId > 0
                ? AppAgentToolRegisterBL.GetBySkillKeyWithLibraries(skillKey, dsId)
                : AppAgentToolRegisterBL.GetBySkillKeyWithLibraries(skillKey);
            var tool = tools?.FirstOrDefault(t =>
                string.Equals(t.ToolName, name, StringComparison.OrdinalIgnoreCase));
            if (tool == null)
                return ToolText("Unknown tool: " + name, true);

            var strArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in args.Properties())
                strArgs[prop.Name] = prop.Value?.Type == JTokenType.String
                    ? (string)prop.Value
                    : prop.Value?.ToString(Formatting.None) ?? "";

            var identity = session.Identity;
            var context = new AgentToolContext
            {
                ConnectionString = identity?.CurrentUserDbConnectionString ?? "",
                DatabaseName     = identity?.CurrentUserDataBaseName ?? "",
                SessionId        = session.SessionId ?? "",
                UserSessionId    = identity?.SessionId?.ToString() ?? "",
                SkillKey         = skillKey,
                UserId           = identity?.UserId != null ? Convert.ToInt32(identity.Value.UserId) : 0,
                CompanyId        = identity?.CurrentWorkingCompanyId != null
                    ? Convert.ToInt32(identity.Value.CurrentWorkingCompanyId)
                    : (session.CompanyId ?? 0),
                DataSourceId     = dsId,
                IsDeterministic  = false
            };

            try
            {
                var result = await AppAgentToolEngine.Dispatch(
                    tool.ToolType, tool.ToolConfig, strArgs, context, ct).ConfigureAwait(false);
                return ToolText(result ?? "");
            }
            catch (Exception ex)
            {
                return ToolText("Tool error: " + ex.Message, true);
            }
        }

        private static object[] ToolDescriptors(CursorCloudAgentSessionStore.SessionData session)
        {
            var list = new List<object>
            {
                Tool("get_session_context", "Current Agent Management session (SkillKey, ids).")
            };

            if (session == null || string.IsNullOrWhiteSpace(session.SkillKey))
                return list.ToArray();

            var dsId = session.Identity?.DataSourceId ?? 0;
            var tools = dsId > 0
                ? AppAgentToolRegisterBL.GetBySkillKeyWithLibraries(session.SkillKey, dsId)
                : AppAgentToolRegisterBL.GetBySkillKeyWithLibraries(session.SkillKey);

            foreach (var t in tools ?? new List<AppAgentToolRegisterDto>())
            {
                if (string.IsNullOrWhiteSpace(t.ToolName)) continue;
                list.Add(ToolFromRegister(t));
            }
            return list.ToArray();
        }

        private static object ToolFromRegister(AppAgentToolRegisterDto t)
        {
            object inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() };
            if (!string.IsNullOrWhiteSpace(t.ParameterSchemaJson))
            {
                try { inputSchema = JToken.Parse(t.ParameterSchemaJson); }
                catch { /* keep empty object schema */ }
            }
            return new
            {
                name = t.ToolName,
                description = string.IsNullOrWhiteSpace(t.Description) ? t.ToolName : t.Description,
                inputSchema
            };
        }

        private static object Tool(string name, string description)
            => new
            {
                name,
                description,
                inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
            };

        private static object ToolText(string text, bool isError = false)
            => new
            {
                content = new[] { new { type = "text", text = text ?? "" } },
                isError
            };

        private static object RpcError(object id, int code, string message)
            => new { jsonrpc = "2.0", id, error = new { code, message } };

        private static object InitializeResult()
            => new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "appai-agent-skill-tools", version = "1.0.0" }
            };
    }
}
