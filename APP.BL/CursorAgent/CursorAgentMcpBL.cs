using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.BL;
using App.BL.AppMgr.AiSkill;
using APP.BL.AppConfigPack;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.CursorAgent
{
    public static class CursorAgentMcpBL
    {
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
                    result = new { tools = ToolDescriptors() };
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

        public static object McpServerSpec(string mcpBaseUrl, string token)
        {
            if (string.IsNullOrWhiteSpace(mcpBaseUrl) || string.IsNullOrWhiteSpace(token))
                return null;
            return new[]
            {
                new
                {
                    name = "appai",
                    type = "http",
                    url = mcpBaseUrl.TrimEnd('/') + "/webapi/CursorAgentMcp/Invoke",
                    headers = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + token }
                    }
                }
            };
        }

        private static object CallToolSync(JObject p)
        {
            return CallToolAsync(p, CancellationToken.None).GetAwaiter().GetResult();
        }

        private static async Task<object> CallToolAsync(JObject p, CancellationToken ct)
        {
            var name = (string)p?["name"];
            var args = p?["arguments"] as JObject ?? new JObject();
            var session = CursorAgentContext.Current;
            if (session == null)
                return ToolText("No Cursor Agent session is bound to this MCP token.", true);

            RestoreIdentity(session);

            try
            {
                switch ((name ?? "").Trim())
                {
                    case "get_session_context":
                        return ToolText(JsonConvert.SerializeObject(new
                        {
                            session.SessionId,
                            session.SaasApplicationId,
                            session.DataSourceRegisterId,
                            session.WorkspaceRelativePath,
                            CursorAgentId = session.CursorAgentId
                        }));
                    case "list_skills":
                        return ToolText(JsonConvert.SerializeObject(CursorAgentSkillBL.ListSkills(SkillDs())));
                    case "get_skill":
                        return ToolText(CursorAgentSkillBL.GetSkill(SkillDs(), Str(args, "name")));
                    case "list_datasources":
                        return ToolText(JsonConvert.SerializeObject(
                            AppDataSourceRegisterBL.GetDataSourceRegisterList()
                                .Select(d => new { d.Id, Name = d.DataSourceName, d.DatabaseName })
                                .ToList()));
                    case "get_table_schema":
                        return ToolText(CursorSqlGateBL.GetTableSchema(
                            RequireDs(args, session),
                            Str(args, "schemaOwner") ?? "dbo",
                            Str(args, "tableName")));
                    case "list_application_assets":
                        return ToolText(ListAssets(session.SaasApplicationId));
                    case "list_workspace_files":
                        return ToolText(JsonConvert.SerializeObject(CursorWorkspaceBL.ListFiles(session.WorkspaceRelativePath, session.CompanyId)));
                    case "read_workspace_file":
                        return ToolText(CursorWorkspaceBL.ReadFile(session.WorkspaceRelativePath, Str(args, "relativePath"), session.CompanyId).Content);
                    case "write_workspace_file":
                        {
                            var rel = CursorWorkspaceBL.WriteFile(session.WorkspaceRelativePath, Str(args, "relativePath"), Str(args, "content") ?? "", session.CompanyId);
                            CursorAgentSessionStore.Enqueue(session.SessionId, new CursorAgentEventDto
                            {
                                EventType = "file",
                                File = new CursorAgentFileEvent { Action = "write", RelativePath = rel }
                            });
                            return ToolText("Wrote " + rel);
                        }
                    case "delete_workspace_file":
                        CursorWorkspaceBL.DeleteFile(session.WorkspaceRelativePath, Str(args, "relativePath"), session.CompanyId);
                        CursorAgentSessionStore.Enqueue(session.SessionId, new CursorAgentEventDto
                        {
                            EventType = "file",
                            File = new CursorAgentFileEvent { Action = "delete", RelativePath = Str(args, "relativePath") }
                        });
                        return ToolText("Deleted " + Str(args, "relativePath"));
                    case "validate_config_pack":
                        return ToolText(ValidatePack(session, Str(args, "relativePath")));
                    case "preview_config_pack":
                        return ToolText(PreviewPack(session, Str(args, "relativePath")));
                    case "run_select":
                        return ToolText(CursorSqlGateBL.RunSelect(
                            RequireDs(args, session),
                            Str(args, "sql"),
                            CursorAgentConfig.SqlPreviewRowLimit));
                    case "propose_import_pack":
                        return ToolText(await ProposeImportAsync(session, Str(args, "relativePath"), ct).ConfigureAwait(false));
                    case "propose_sql":
                        return ToolText(await ProposeSqlAsync(session, Str(args, "sql"), RequireDs(args, session), ct).ConfigureAwait(false));
                    default:
                        return ToolText("Unknown tool: " + name, true);
                }
            }
            catch (Exception ex)
            {
                return ToolText(ex.Message, true);
            }
        }

        private static async Task<string> ProposeImportAsync(CursorAgentSessionStore.SessionData session, string relativePath, CancellationToken ct)
        {
            var preview = PreviewPack(session, relativePath);
            var gate = new CursorAgentGateEvent
            {
                GateId = Guid.NewGuid().ToString("N"),
                Kind = "import_pack",
                Title = "Import Config Pack",
                Summary = "Import " + relativePath + " into application " + session.SaasApplicationId,
                RelativePath = relativePath,
                Preview = SafeJson(preview)
            };
            return await WaitGateAsync(session, gate, confirmed =>
            {
                RestoreIdentity(session);
                var pack = LoadPack(session, relativePath);
                var exec = AppConfigPackBL.Execute(new AppConfigPackExecuteRequestDto
                {
                    Pack = pack,
                    SaasApplicationId = session.SaasApplicationId
                });
                var text = JsonConvert.SerializeObject(exec.Object);
                TryWriteOutput(session, "output/last-import.json", text);
                return text;
            }, ct).ConfigureAwait(false);
        }

        private static async Task<string> ProposeSqlAsync(CursorAgentSessionStore.SessionData session, string sql, int dsId, CancellationToken ct)
        {
            var classified = CursorSqlGateBL.Classify(sql);
            if (!classified.Allowed || classified.IsReadOnly)
                return classified.Reason ?? "SQL not allowed for propose_sql. Use run_select for SELECT.";

            var gate = new CursorAgentGateEvent
            {
                GateId = Guid.NewGuid().ToString("N"),
                Kind = "exec_sql",
                Title = "Execute " + classified.Kind,
                Summary = classified.Kind + " against DataSource " + dsId,
                Sql = classified.Normalized,
                DataSourceRegisterId = dsId
            };
            return await WaitGateAsync(session, gate, confirmed =>
            {
                RestoreIdentity(session);
                var text = CursorSqlGateBL.ExecuteWrite(dsId, classified.Normalized);
                TryWriteOutput(session, "output/last-sql.json", text);
                return text;
            }, ct).ConfigureAwait(false);
        }

        private static async Task<string> WaitGateAsync(
            CursorAgentSessionStore.SessionData session,
            CursorAgentGateEvent gate,
            Func<bool, string> onConfirm,
            CancellationToken ct)
        {
            var tcs = CursorAgentSessionStore.RegisterGate(session.SessionId, gate);
            CursorAgentSessionStore.Enqueue(session.SessionId, new CursorAgentEventDto { EventType = "gate", Gate = gate });
            CursorAgentSessionBL.Update(session, "InProgress", null, gate);

            using (var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token))
            {
                linked.Token.Register(() => tcs.TrySetResult(new CursorAgentGateResult
                {
                    Confirmed = false,
                    Feedback = "Gate timed out."
                }));
                var decision = await tcs.Task.ConfigureAwait(false);
                if (!decision.Confirmed)
                    return "User rejected: " + (decision.Feedback ?? "no feedback");
                return onConfirm(true);
            }
        }

        private static string ValidatePack(CursorAgentSessionStore.SessionData session, string relativePath)
        {
            var pack = LoadPack(session, relativePath);
            var result = AppConfigPackBL.Validate(pack);
            return JsonConvert.SerializeObject(result.Object);
        }

        private static string PreviewPack(CursorAgentSessionStore.SessionData session, string relativePath)
        {
            var pack = LoadPack(session, relativePath);
            var result = AppConfigPackBL.Preview(new AppConfigPackExecuteRequestDto
            {
                Pack = pack,
                SaasApplicationId = session.SaasApplicationId
            });
            return JsonConvert.SerializeObject(result.Object);
        }

        private static AppConfigPackDto LoadPack(CursorAgentSessionStore.SessionData session, string relativePath)
        {
            var file = CursorWorkspaceBL.ReadFile(session.WorkspaceRelativePath, relativePath, session.CompanyId);
            var loaded = AppConfigPackBL.Load(new AppConfigPackLoadRequestDto { PackJson = file.Content });
            if (loaded.Object == null)
                throw new InvalidOperationException("Invalid pack JSON: " + (loaded.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "unknown"));
            return loaded.Object;
        }

        private static string ListAssets(int? saasApplicationId)
        {
            if (!saasApplicationId.HasValue)
                return JsonConvert.SerializeObject(new { Error = "SaasApplicationId is required." });
            var txs = AppTransactionBL.RetrieveSaasApplicationTransactionList(saasApplicationId.Value)
                .Select(t => new { t.Id, Name = t.TransactionName })
                .ToList();
            var searches = AppSearchConfigBL.RetrieveSaasApplicationSearchList(saasApplicationId.Value)
                .Select(s => new { s.Id, s.Name })
                .ToList();
            return JsonConvert.SerializeObject(new { Transactions = txs, Searches = searches });
        }

        private static void TryWriteOutput(CursorAgentSessionStore.SessionData session, string path, string content)
        {
            try { CursorWorkspaceBL.WriteFile(session.WorkspaceRelativePath, path, content, session.CompanyId); }
            catch { }
        }

        private static void RestoreIdentity(CursorAgentSessionStore.SessionData session)
        {
            CursorAgentIdentity.Restore(session);
        }

        private static int? SkillDs()
        {
            return AppAISkillBL.GetDefaultDataSourceId();
        }

        private static int RequireDs(JObject args, CursorAgentSessionStore.SessionData session)
        {
            var raw = args?["dataSourceRegisterId"];
            if (raw != null && raw.Type != JTokenType.Null)
                return raw.Value<int>();
            if (session.DataSourceRegisterId.HasValue)
                return session.DataSourceRegisterId.Value;
            throw new InvalidOperationException("dataSourceRegisterId is required (or pick a default DataSource in the UI).");
        }

        private static string Str(JObject args, string name)
        {
            return (string)args?[name];
        }

        private static object SafeJson(string json)
        {
            try { return JsonConvert.DeserializeObject(json); }
            catch { return json; }
        }

        private static object ToolText(string text, bool isError = false)
        {
            return new
            {
                content = new[] { new { type = "text", text = text ?? "" } },
                isError
            };
        }

        private static object RpcError(object id, int code, string message)
        {
            return new { jsonrpc = "2.0", id, error = new { code, message } };
        }

        private static object InitializeResult()
        {
            return new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "appai-cursor-agent", version = "1.0.0" }
            };
        }

        private static object[] ToolDescriptors()
        {
            return new object[]
            {
                Tool("get_session_context", "Current Application, DataSource, and workspace path."),
                Tool("list_skills", "List Cursor Agent skills stored in AppAISkill."),
                Tool("get_skill", "Load a composed skill by name.", Prop("name", "string", true)),
                Tool("list_datasources", "List tenant registered databases."),
                Tool("get_table_schema", "Columns/PK for a table.",
                    Prop("tableName", "string", true), Prop("schemaOwner", "string", false), Prop("dataSourceRegisterId", "integer", false)),
                Tool("list_application_assets", "Existing transactions and searches on the selected Application."),
                Tool("list_workspace_files", "List files in the session workspace."),
                Tool("read_workspace_file", "Read a workspace file.", Prop("relativePath", "string", true)),
                Tool("write_workspace_file", "Write a workspace file.", Prop("relativePath", "string", true), Prop("content", "string", true)),
                Tool("delete_workspace_file", "Delete a workspace file.", Prop("relativePath", "string", true)),
                Tool("validate_config_pack", "Validate an AppConfigPack JSON file.", Prop("relativePath", "string", true)),
                Tool("preview_config_pack", "Preview import actions without applying.", Prop("relativePath", "string", true)),
                Tool("propose_import_pack", "Ask the user to confirm importing a pack. Blocks until they confirm.", Prop("relativePath", "string", true)),
                Tool("run_select", "Run a SELECT (row-capped).", Prop("sql", "string", true), Prop("dataSourceRegisterId", "integer", false)),
                Tool("propose_sql", "Ask the user to confirm INSERT/UPDATE/DELETE/CREATE TABLE/ALTER TABLE ADD.", Prop("sql", "string", true), Prop("dataSourceRegisterId", "integer", false))
            };
        }

        private static object Tool(string name, string description, params ToolProp[] props)
        {
            var properties = new Dictionary<string, object>();
            var required = new List<string>();
            var list = props ?? new ToolProp[0];
            foreach (var p in list)
            {
                properties[p.Name] = new { type = p.Type };
                if (p.Required) required.Add(p.Name);
            }
            return new
            {
                name,
                description,
                inputSchema = new
                {
                    type = "object",
                    properties,
                    required
                }
            };
        }

        private static ToolProp Prop(string name, string type, bool required)
        {
            return new ToolProp { Name = name, Type = type, Required = required };
        }

        private sealed class ToolProp
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool Required { get; set; }
        }
    }

    public static class CursorAgentContext
    {
        private static readonly AsyncLocal<CursorAgentSessionStore.SessionData> CurrentData
            = new AsyncLocal<CursorAgentSessionStore.SessionData>();

        public static CursorAgentSessionStore.SessionData Current
        {
            get { return CurrentData.Value; }
            set { CurrentData.Value = value; }
        }
    }
}
