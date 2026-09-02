using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.BL;
using App.BL.AppMgr.AiSkill;
using APP.BL.AppConfigPack;
using APP.Components.EntityDto;
using APP.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationAgentMcpBL
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
                    url = mcpBaseUrl.TrimEnd('/') + "/webapi/AppDataIntegrationAgentMcp/Invoke",
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
            var session = AppDataIntegrationAgentContext.Current;
            if (session == null)
                return ToolText("No App Data Integration Agent session is bound to this MCP token.", true);

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
                            AllowedDataSources = AppDataIntegrationAgentDataSourceBL.ListForSession(session),
                            session.WorkspaceRelativePath,
                            session.SkillKey,
                            session.AllowProposeImport,
                            CloudAgentId = session.CloudAgentId
                        }));
                    case "list_skills":
                        return ToolText(JsonConvert.SerializeObject(AppDataIntegrationAgentSkillCatalogBL.ListMenu()));
                    case "get_skill":
                        return ToolText(AppDataIntegrationAgentSkillBL.GetSkill(SkillDs(), Str(args, "name")));
                    case "list_datasources":
                        return ToolText(JsonConvert.SerializeObject(
                            AppDataIntegrationAgentDataSourceBL.ListForSession(session)));
                    case "get_table_schema":
                        return ToolText(AppDataIntegrationSqlGateBL.GetTableSchema(
                            AppDataIntegrationAgentDataSourceBL.ResolveSqlTarget(session, args),
                            Str(args, "schemaOwner") ?? "dbo",
                            Str(args, "tableName")));
                    case "list_application_assets":
                        return ToolText(ListAssets(session.SaasApplicationId));
                    case "list_application_menus":
                        return ToolText(ListApplicationMenus(session.SaasApplicationId));
                    case "open_app_page":
                        return ToolText(EnqueueNavigate(session, args));
                    case "open_search":
                        return ToolText(OpenSearch(session, args));
                    case "open_list_edit_form":
                        return ToolText(OpenTransactionForm(session, args, listEdit: true));
                    case "open_master_detail_form":
                        return ToolText(OpenTransactionForm(session, args, listEdit: false));
                    case "open_transaction_editor":
                        return ToolText(OpenTransactionEditor(session, args, "TransactionGraphicEditor"));
                    case "open_form_design":
                        return ToolText(OpenTransactionEditor(session, args, "Form"));
                    case "open_search_editor":
                        return ToolText(OpenSearchEditor(session, args));
                    case "open_entity_editor":
                        return ToolText(OpenSimplePage(session, "entity-info-edit", "Entity Editor",
                            ResolveEntityId(args), "entityId"));
                    case "open_er_diagram":
                        return ToolText(OpenSimplePage(session, "er-diagram-editor", "ER Diagram",
                            ResolveIntId(args, "diagramId", "id", "erDiagramId"), "id"));
                    case "open_database_design":
                        return ToolText(OpenDatabaseDesign(session, args));
                    case "open_query_result":
                        return ToolText(OpenQueryResult(session, args));
                    case "preview_tables_data":
                        return ToolText(EnqueueTablePreview(session, args));
                    case "list_workspace_files":
                        return ToolText(JsonConvert.SerializeObject(AppDataIntegrationWorkspaceBL.ListFiles(session.WorkspaceRelativePath, session.CompanyId)));
                    case "read_workspace_file":
                        return ToolText(AppDataIntegrationWorkspaceBL.ReadFile(session.WorkspaceRelativePath, Str(args, "relativePath"), session.CompanyId).Content);
                    case "write_workspace_file":
                    case "append_workspace_file":
                        return ToolText(McpWriteDisabledMessage(Str(args, "relativePath")), true);
                    case "sync_cloud_artifacts":
                        return ToolText(await AppDataIntegrationAgentBL.SyncCloudArtifactsAsync(session, ct)
                            .ConfigureAwait(false));
                    case "delete_workspace_file":
                        AppDataIntegrationWorkspaceBL.DeleteFile(session.WorkspaceRelativePath, Str(args, "relativePath"), session.CompanyId);
                        AppDataIntegrationAgentSessionStore.Enqueue(session.SessionId, new AppDataIntegrationAgentEventDto
                        {
                            EventType = "file",
                            File = new AppDataIntegrationAgentFileEvent { Action = "delete", RelativePath = Str(args, "relativePath") }
                        });
                        return ToolText("Deleted " + Str(args, "relativePath"));
                    case "validate_config_pack":
                        {
                            var rel = Str(args, "relativePath");
                            AppDataIntegrationAgentSessionStore.NotePackPath(session, rel);
                            return ToolText(ValidatePack(session, rel));
                        }
                    case "preview_config_pack":
                        {
                            var rel = Str(args, "relativePath");
                            AppDataIntegrationAgentSessionStore.NotePackPath(session, rel);
                            return ToolText(PreviewPack(session, rel));
                        }
                    case "run_select":
                        return ToolText(AppDataIntegrationSqlGateBL.RunSelect(
                            AppDataIntegrationAgentDataSourceBL.ResolveSqlTarget(session, args),
                            Str(args, "sql"),
                            AppDataIntegrationAgentConfig.SqlPreviewRowLimit));
                    case "propose_import_pack":
                        if (!session.AllowProposeImport)
                            return ToolText("propose_import_pack is disabled for this skill. Write the pack to packs/ instead.", true);
                        return ToolText(await ProposeImportAsync(session, Str(args, "relativePath"), ct).ConfigureAwait(false));
                    case "propose_sql":
                        return ToolText(await ProposeSqlAsync(session, Str(args, "sql"),
                            AppDataIntegrationAgentDataSourceBL.ResolveSqlTarget(session, args), ct).ConfigureAwait(false));
                    default:
                        return ToolText("Unknown tool: " + name, true);
                }
            }
            catch (Exception ex)
            {
                return ToolText(ex.Message, true);
            }
        }

        private static async Task<string> ProposeImportAsync(AppDataIntegrationAgentSessionStore.SessionData session, string relativePath, CancellationToken ct)
        {
            var preview = PreviewPack(session, relativePath);
            var gate = new AppDataIntegrationAgentGateEvent
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

        private static async Task<string> ProposeSqlAsync(
            AppDataIntegrationAgentSessionStore.SessionData session,
            string sql,
            AppDataIntegrationAgentSqlTarget target,
            CancellationToken ct)
        {
            var classified = AppDataIntegrationSqlGateBL.Classify(sql);
            if (!classified.Allowed || classified.IsReadOnly)
                return classified.Reason ?? "SQL not allowed for propose_sql. Use run_select for SELECT.";

            var summaryTarget = target.UsesConnectionString
                ? "user-supplied connection string"
                : "DataSource " + target.DataSourceRegisterId;
            var gate = new AppDataIntegrationAgentGateEvent
            {
                GateId = Guid.NewGuid().ToString("N"),
                Kind = "exec_sql",
                Title = "Execute " + classified.Kind,
                Summary = classified.Kind + " against " + summaryTarget,
                Sql = classified.Normalized,
                DataSourceRegisterId = target.DataSourceRegisterId
            };
            return await WaitGateAsync(session, gate, confirmed =>
            {
                RestoreIdentity(session);
                var text = AppDataIntegrationSqlGateBL.ExecuteWrite(target, classified.Normalized);
                TryWriteOutput(session, "output/last-sql.json", text);
                return text;
            }, ct).ConfigureAwait(false);
        }

        private static async Task<string> WaitGateAsync(
            AppDataIntegrationAgentSessionStore.SessionData session,
            AppDataIntegrationAgentGateEvent gate,
            Func<bool, string> onConfirm,
            CancellationToken ct)
        {
            var tcs = AppDataIntegrationAgentSessionStore.RegisterGate(session.SessionId, gate);
            AppDataIntegrationAgentSessionStore.Enqueue(session.SessionId, new AppDataIntegrationAgentEventDto { EventType = "gate", Gate = gate });
            AppDataIntegrationAgentSessionBL.Update(session, "InProgress", null, gate);

            using (var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token))
            {
                linked.Token.Register(() => tcs.TrySetResult(new AppDataIntegrationAgentGateResult
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

        private static string ValidatePack(AppDataIntegrationAgentSessionStore.SessionData session, string relativePath)
        {
            var pack = LoadPack(session, relativePath);
            var result = AppConfigPackBL.Validate(pack);
            return JsonConvert.SerializeObject(result.Object);
        }

        private static string PreviewPack(AppDataIntegrationAgentSessionStore.SessionData session, string relativePath)
        {
            var pack = LoadPack(session, relativePath);
            var result = AppConfigPackBL.Preview(new AppConfigPackExecuteRequestDto
            {
                Pack = pack,
                SaasApplicationId = session.SaasApplicationId
            });
            return JsonConvert.SerializeObject(result.Object);
        }

        private static AppConfigPackDto LoadPack(AppDataIntegrationAgentSessionStore.SessionData session, string relativePath)
        {
            var file = AppDataIntegrationWorkspaceBL.ReadFile(session.WorkspaceRelativePath, relativePath, session.CompanyId);
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
                .Select(t => new
                {
                    t.Id,
                    Name = t.TransactionName,
                    OrganizedType = t.TransactionOrganizedType
                })
                .ToList();
            var searches = AppSearchConfigBL.RetrieveSaasApplicationSearchList(saasApplicationId.Value)
                .Select(s => new { s.Id, s.Name })
                .ToList();
            return JsonConvert.SerializeObject(new { Transactions = txs, Searches = searches });
        }

        private static string ListApplicationMenus(int? saasApplicationId)
        {
            if (!saasApplicationId.HasValue)
                return JsonConvert.SerializeObject(new { Error = "SaasApplicationId is required." });
            var root = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, saasApplicationId.Value);
            var flat = new List<object>();
            void Walk(IEnumerable<AppListMenuExDto> nodes)
            {
                if (nodes == null) return;
                foreach (var n in nodes)
                {
                    if (n == null) continue;
                    if (!string.IsNullOrWhiteSpace(n.RouteCode))
                    {
                        flat.Add(new
                        {
                            n.Id,
                            n.Name,
                            n.RouteCode,
                            n.Link,
                            ParentId = n.ParentId
                        });
                    }
                    Walk(n.AppListMenu_List);
                }
            }
            Walk(root);
            return JsonConvert.SerializeObject(new { Menus = flat });
        }

        private static string EnqueueNavigate(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            var routeCode = Str(args, "routeCode") ?? Str(args, "route");
            if (string.IsNullOrWhiteSpace(routeCode))
                return "routeCode is required.";
            var label = Str(args, "label") ?? routeCode;
            var link = Str(args, "link");
            Dictionary<string, object> paramObj = null;
            var rawParams = args?["paramObj"];
            if (rawParams != null && rawParams.Type != JTokenType.Null)
            {
                paramObj = rawParams.ToObject<Dictionary<string, object>>()
                    ?? new Dictionary<string, object>();
            }

            AppDataIntegrationAgentSessionStore.NoteOpenUiOffer(session, new AppDataIntegrationAgentOpenUiOfferDto
            {
                Kind = "navigate",
                Label = label,
                RouteCode = routeCode.Trim(),
                Link = link,
                ParamObj = paramObj
            });
            AppDataIntegrationAgentSessionStore.Enqueue(session.SessionId, new AppDataIntegrationAgentEventDto
            {
                EventType = "navigate",
                Navigate = new AppDataIntegrationAgentNavigateEvent
                {
                    RouteCode = routeCode.Trim(),
                    Label = label,
                    Link = link,
                    ParamObj = paramObj
                }
            });
            return JsonConvert.SerializeObject(new
            {
                Ok = true,
                Message = "Open page offered in chat. The user must click Open — do not assume the page is already open.",
                RouteCode = routeCode,
                Label = label,
                Link = link,
                ParamObj = paramObj
            });
        }

        private static string OpenSearch(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            int? searchId = ResolveSearchId(session, args);
            if (!searchId.HasValue)
                return "Could not resolve search. Pass searchId, integrationId, or name.";
            var name = Str(args, "label") ?? ("Search #" + searchId);
            return EnqueueNavigate(session, JObject.FromObject(new
            {
                routeCode = "MasterDataManagement",
                label = name,
                link = searchId.Value.ToString(),
                paramObj = new { searchId = searchId.Value }
            }));
        }

        private static string OpenTransactionForm(
            AppDataIntegrationAgentSessionStore.SessionData session,
            JObject args,
            bool listEdit)
        {
            int? txId = ResolveTransactionId(session, args);
            if (!txId.HasValue)
                return "Could not resolve transaction. Pass transactionId, integrationId, or name.";
            var route = listEdit ? "FormListEdit" : "FormMasterDetail";
            var label = Str(args, "label") ?? ((listEdit ? "ListEdit #" : "Form #") + txId);
            var paramObj = new Dictionary<string, object> { ["id"] = txId.Value.ToString() };
            var rootPk = Str(args, "rootPrimaryKey") ?? Str(args, "param1");
            if (!string.IsNullOrWhiteSpace(rootPk))
                paramObj["param1"] = rootPk;
            var param2 = Str(args, "param2");
            if (!string.IsNullOrWhiteSpace(param2))
                paramObj["param2"] = param2;
            return EnqueueNavigate(session, JObject.FromObject(new
            {
                routeCode = route,
                label,
                link = txId.Value.ToString(),
                paramObj
            }));
        }

        private static string OpenTransactionEditor(
            AppDataIntegrationAgentSessionStore.SessionData session,
            JObject args,
            string defaultSectionCode)
        {
            int? txId = ResolveTransactionId(session, args);
            if (!txId.HasValue)
                return "Could not resolve transaction. Pass transactionId, integrationId, or name.";
            if (!session.SaasApplicationId.HasValue)
                return "SaasApplicationId is required on the session.";
            var label = Str(args, "label") ?? ("Transaction #" + txId);
            var section = Str(args, "defaultSectionCode") ?? defaultSectionCode;
            return EnqueueNavigate(session, JObject.FromObject(new
            {
                routeCode = "application-form-builder",
                label,
                paramObj = new Dictionary<string, object>
                {
                    ["id"] = session.SaasApplicationId.Value.ToString(),
                    ["transactionId"] = txId.Value,
                    ["defaultSectionCode"] = section,
                    ["isCreateNewItem"] = false
                }
            }));
        }

        private static string OpenSearchEditor(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            int? searchId = ResolveSearchId(session, args);
            if (!searchId.HasValue)
                return "Could not resolve search. Pass searchId, integrationId, or name.";
            var label = Str(args, "label") ?? ("Search Editor #" + searchId);
            return EnqueueNavigate(session, JObject.FromObject(new
            {
                routeCode = "search-editor",
                label,
                link = searchId.Value.ToString(),
                paramObj = new { id = searchId.Value.ToString() }
            }));
        }

        private static string OpenSimplePage(
            AppDataIntegrationAgentSessionStore.SessionData session,
            string routeCode,
            string defaultLabel,
            int? id,
            string idKey)
        {
            if (!id.HasValue)
                return "id is required for " + routeCode + ".";
            var paramObj = new Dictionary<string, object> { [idKey] = id.Value };
            if (!string.Equals(idKey, "id", StringComparison.OrdinalIgnoreCase))
                paramObj["id"] = id.Value.ToString();
            return EnqueueNavigate(session, JObject.FromObject(new
            {
                routeCode,
                label = defaultLabel + " #" + id.Value,
                link = id.Value.ToString(),
                paramObj
            }));
        }

        private static string OpenDatabaseDesign(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            var appId = ResolveIntId(args, "applicationId", "saasApplicationId", "id")
                ?? session.SaasApplicationId;
            var dsId = ResolveIntId(args, "dataSourceRegisterId", "dataSourceId")
                ?? session.DataSourceRegisterId;
            var paramObj = new Dictionary<string, object>();
            if (appId.HasValue) paramObj["id"] = appId.Value.ToString();
            if (dsId.HasValue) paramObj["dataSourceRegisterId"] = dsId.Value;
            return EnqueueNavigate(session, JObject.FromObject(new
            {
                routeCode = "database-design-management",
                label = "Database Design",
                paramObj
            }));
        }

        /// <summary>
        /// Offer Open → SQL Workbench with queryText (+ optional DS). Page auto-runs SELECT when opened.
        /// </summary>
        private static string OpenQueryResult(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            var sql = Str(args, "sql") ?? Str(args, "queryText") ?? Str(args, "query");
            if (string.IsNullOrWhiteSpace(sql))
                return "sql (or queryText) is required.";
            sql = sql.Trim();
            if (sql.Length > 12000)
                return "sql is too long for the Open URL (max ~12000 chars). Shorten the query or ask the user to run it in SQL Workbench manually.";

            if (AppDataIntegrationAgentDataSourceBL.ShouldSkipSqlWorkbenchOpen(session, args))
            {
                return JsonConvert.SerializeObject(new
                {
                    Ok = false,
                    SqlWorkbenchOpenAvailable = false,
                    Message = "SQL Workbench Open is not available: the query used a user-supplied connection string "
                        + "(or an unregistered database), not a tenant DataSourceRegister row. "
                        + "Summarize the result in chat only — do NOT mention an Open button or call open_query_result again."
                });
            }

            var dsId = AppDataIntegrationAgentDataSourceBL.ResolveForTool(session,
                ResolveIntId(args, "dataSourceRegisterId", "dataSourceId") ?? session.DataSourceRegisterId);
            var autoExecute = true;
            var autoTok = args?["autoExecute"];
            if (autoTok != null && autoTok.Type != JTokenType.Null)
            {
                if (autoTok.Type == JTokenType.Boolean) autoExecute = autoTok.Value<bool>();
                else if (string.Equals(autoTok.ToString(), "false", StringComparison.OrdinalIgnoreCase))
                    autoExecute = false;
            }

            var paramObj = new Dictionary<string, object>
            {
                ["queryText"] = sql,
                ["autoExecute"] = autoExecute
            };
            if (dsId > 0) paramObj["dataSourceRegisterId"] = dsId;

            var label = Str(args, "label");
            if (string.IsNullOrWhiteSpace(label))
            {
                var oneLine = sql.Replace("\r", " ").Replace("\n", " ").Trim();
                if (oneLine.Length > 48) oneLine = oneLine.Substring(0, 45) + "...";
                label = "SQL Workbench — " + oneLine;
            }

            return EnqueueNavigate(session, JObject.FromObject(new
            {
                routeCode = "database-design-management",
                label,
                paramObj
            }));
        }

        private static string EnqueueTablePreview(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            var tables = new List<AppDataIntegrationAgentTablePreviewItemDto>();
            var arr = args?["tables"] as JArray;
            if (arr != null)
            {
                foreach (var t in arr)
                {
                    if (t == null || t.Type == JTokenType.Null) continue;
                    var name = (string)t["tableName"] ?? (string)t["name"];
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    int? ds = null;
                    var dsTok = t["dataSourceId"] ?? t["dataSourceRegisterId"];
                    if (dsTok != null && dsTok.Type != JTokenType.Null)
                        ds = dsTok.Value<int>();
                    if (!ds.HasValue)
                        ds = session.DataSourceRegisterId;
                    tables.Add(new AppDataIntegrationAgentTablePreviewItemDto
                    {
                        TableName = name.Trim(),
                        DataSourceId = ds,
                        SchemaOwner = (string)t["schemaOwner"] ?? "dbo"
                    });
                }
            }

            // Convenience: single table via top-level args
            if (tables.Count == 0)
            {
                var one = Str(args, "tableName");
                if (!string.IsNullOrWhiteSpace(one))
                {
                    tables.Add(new AppDataIntegrationAgentTablePreviewItemDto
                    {
                        TableName = one.Trim(),
                        DataSourceId = ResolveIntId(args, "dataSourceRegisterId", "dataSourceId")
                            ?? session.DataSourceRegisterId,
                        SchemaOwner = Str(args, "schemaOwner") ?? "dbo"
                    });
                }
            }

            if (tables.Count == 0)
                return "tables[] (or tableName) is required.";

            AppDataIntegrationAgentDataSourceBL.EnsureTablePreviewDataSources(session, tables);

            var names = string.Join(", ", tables.Select(t => t.TableName).Where(n => !string.IsNullOrWhiteSpace(n)));
            AppDataIntegrationAgentSessionStore.NoteOpenUiOffer(session, new AppDataIntegrationAgentOpenUiOfferDto
            {
                Kind = "table_preview",
                Label = string.IsNullOrWhiteSpace(names) ? "Table Preview" : names,
                Tables = tables
            });
            AppDataIntegrationAgentSessionStore.Enqueue(session.SessionId, new AppDataIntegrationAgentEventDto
            {
                EventType = "table_preview",
                TablePreview = new AppDataIntegrationAgentTablePreviewEvent { Tables = tables }
            });
            return JsonConvert.SerializeObject(new
            {
                Ok = true,
                Message = "Table Preview offered in chat (Open button). The user must click Open — the modal does not open by itself.",
                Tables = tables
            });
        }

        private static int? ResolveTransactionId(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            var id = ResolveIntId(args, "transactionId", "id");
            if (id.HasValue) return id;
            var integrationId = Str(args, "integrationId");
            if (!string.IsNullOrWhiteSpace(integrationId))
            {
                var byIntegration = LookupIdByIntegration("AppTransaction", "TransactionID", integrationId);
                if (byIntegration.HasValue) return byIntegration;
            }
            var name = Str(args, "name") ?? Str(args, "transactionName");
            if (!string.IsNullOrWhiteSpace(name) && session.SaasApplicationId.HasValue)
            {
                var match = AppTransactionBL.RetrieveSaasApplicationTransactionList(session.SaasApplicationId.Value)
                    .FirstOrDefault(t => string.Equals(t.TransactionName, name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match?.Id != null) return Convert.ToInt32(match.Id);
            }
            return null;
        }

        private static int? ResolveSearchId(AppDataIntegrationAgentSessionStore.SessionData session, JObject args)
        {
            var id = ResolveIntId(args, "searchId", "id");
            if (id.HasValue) return id;
            var integrationId = Str(args, "integrationId");
            if (!string.IsNullOrWhiteSpace(integrationId))
            {
                var byIntegration = LookupIdByIntegration("AppSearch", "SearchID", integrationId);
                if (byIntegration.HasValue) return byIntegration;
            }
            var name = Str(args, "name") ?? Str(args, "searchName");
            if (!string.IsNullOrWhiteSpace(name) && session.SaasApplicationId.HasValue)
            {
                var match = AppSearchConfigBL.RetrieveSaasApplicationSearchList(session.SaasApplicationId.Value)
                    .FirstOrDefault(s => string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match?.Id != null) return Convert.ToInt32(match.Id);
            }
            return null;
        }

        private static int? LookupIdByIntegration(string table, string idColumn, string integrationId)
        {
            if (string.IsNullOrWhiteSpace(integrationId)) return null;
            try
            {
                // AppTransaction / AppSearch live on the tenant metadata DB (ServerContext.DataSourceId),
                // not the session business DataSourceRegisterId.
                int dsId = ServerContext.Instance != null && ServerContext.Instance.DataSourceId > 0
                    ? ServerContext.Instance.DataSourceId
                    : AppDataSourceRegisterBL.GetDefaultDataSourceRegId() ?? 0;
                if (dsId <= 0) return null;
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dsId);
                var p = fixture.CreateParameter("@IntegrationId");
                p.Value = integrationId.Trim();
                var dt = fixture.RetriveDataTable(
                    "SELECT TOP 1 " + idColumn + " FROM dbo." + table + " WHERE IntegrationId = @IntegrationId",
                    new List<DbParameter> { p });
                if (dt != null && dt.Rows.Count > 0)
                    return Convert.ToInt32(dt.Rows[0][0]);
            }
            catch { }
            return null;
        }

        private static int? ResolveEntityId(JObject args)
        {
            return ResolveIntId(args, "entityId", "id");
        }

        private static int? ResolveIntId(JObject args, params string[] names)
        {
            if (args == null || names == null) return null;
            foreach (var name in names)
            {
                var tok = args[name];
                if (tok == null || tok.Type == JTokenType.Null) continue;
                if (tok.Type == JTokenType.Integer) return tok.Value<int>();
                int parsed;
                if (int.TryParse(tok.ToString(), out parsed)) return parsed;
            }
            return null;
        }

        private static void TryWriteOutput(AppDataIntegrationAgentSessionStore.SessionData session, string path, string content)
        {
            try { AppDataIntegrationWorkspaceBL.WriteFile(session.WorkspaceRelativePath, path, content, session.CompanyId); }
            catch { }
        }

        private static void RestoreIdentity(AppDataIntegrationAgentSessionStore.SessionData session)
        {
            AppDataIntegrationAgentIdentity.Restore(session);
        }

        private static int? SkillDs()
        {
            return AppAISkillBL.GetDefaultDataSourceId();
        }

        private static int RequireDs(JObject args, AppDataIntegrationAgentSessionStore.SessionData session)
        {
            var raw = args?["dataSourceRegisterId"] ?? args?["dataSourceId"];
            int? requested = null;
            if (raw != null && raw.Type != JTokenType.Null)
                requested = raw.Value<int>();
            return AppDataIntegrationAgentDataSourceBL.ResolveForTool(session, requested);
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

        private static string McpWriteDisabledMessage(string relativePath)
        {
            var path = string.IsNullOrWhiteSpace(relativePath) ? "<relativePath>" : relativePath.Replace('\\', '/').TrimStart('/');
            return "MCP write_workspace_file / append_workspace_file are DISABLED for delivering files to App workspace. "
                + "Same path as generated images: (1) write the file on the cloud VM under the Cursor artifacts tree "
                + "(e.g. /opt/cursor/artifacts/" + path + " or artifacts/" + path + "), "
                + "(2) call sync_cloud_artifacts, (3) list_workspace_files and check SizeBytes. "
                + "Do not put file contents in MCP tool arguments.";
        }

        private static string WorkspaceWriteToolMessage(string relativePath, long sizeBytes, bool appended)
        {
            return McpWriteDisabledMessage(relativePath);
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
                serverInfo = new { name = "appai-app-data-integration-agent", version = "1.0.0" }
            };
        }

        private static object[] ToolDescriptors()
        {
            var tools = new List<object>
            {
                Tool("get_session_context", "Current Application, DataSource, skill, and workspace path."),
                Tool("list_skills", "List App Data Integration Agent catalog and other skills."),
                Tool("get_skill", "Load a composed saved skill by name.", Prop("name", "string", true)),
                Tool("list_datasources", "List databases registered for the logged-in tenant company (Database Registration / DataSourceOwnerCompanyId)."),
                Tool("get_table_schema", "Columns/PK for a table.",
                    Prop("tableName", "string", true), Prop("schemaOwner", "string", false),
                    Prop("dataSourceRegisterId", "integer", false), Prop("connectionString", "string", false)),
                Tool("list_application_assets", "Existing transactions and searches on the selected Application."),
                Tool("list_application_menus", "Flat list of main-menu items (RouteCode/Link) for the Application."),
                Tool("open_app_page", "Offer an Open button in chat to open an App tab (routeCode + optional paramObj). Does not open until the user clicks Open.",
                    Prop("routeCode", "string", true), Prop("label", "string", false), Prop("link", "string", false), Prop("paramObj", "object", false)),
                Tool("open_search", "Offer Open for Search runtime (MasterDataManagement).",
                    Prop("searchId", "integer", false), Prop("integrationId", "string", false), Prop("name", "string", false), Prop("label", "string", false)),
                Tool("open_list_edit_form", "Offer Open for ListEdit form runtime (FormListEdit).",
                    Prop("transactionId", "integer", false), Prop("integrationId", "string", false), Prop("name", "string", false), Prop("label", "string", false)),
                Tool("open_master_detail_form", "Offer Open for MasterDetail form runtime (FormMasterDetail).",
                    Prop("transactionId", "integer", false), Prop("integrationId", "string", false), Prop("name", "string", false),
                    Prop("rootPrimaryKey", "string", false), Prop("param2", "string", false), Prop("label", "string", false)),
                Tool("open_transaction_editor", "Offer Open for Application Form Builder (Transaction Graphic Editor).",
                    Prop("transactionId", "integer", false), Prop("integrationId", "string", false), Prop("name", "string", false), Prop("label", "string", false)),
                Tool("open_form_design", "Offer Open for Application Form Builder (Form design).",
                    Prop("transactionId", "integer", false), Prop("integrationId", "string", false), Prop("name", "string", false), Prop("label", "string", false)),
                Tool("open_search_editor", "Offer Open for Search editor.",
                    Prop("searchId", "integer", false), Prop("integrationId", "string", false), Prop("name", "string", false), Prop("label", "string", false)),
                Tool("open_entity_editor", "Offer Open for Entity Info editor.", Prop("entityId", "integer", true)),
                Tool("open_er_diagram", "Offer Open for ER Diagram editor.", Prop("diagramId", "integer", true)),
                Tool("open_database_design", "Offer Open for Database Design management.",
                    Prop("applicationId", "integer", false), Prop("dataSourceRegisterId", "integer", false)),
                Tool("open_query_result", "Offer Open for SQL Workbench — only when the query used a registered tenant dataSourceRegisterId (not connectionString).",
                    Prop("sql", "string", true), Prop("dataSourceRegisterId", "integer", false),
                    Prop("autoExecute", "boolean", false), Prop("label", "string", false)),
                Tool("preview_tables_data", "Offer an Open button for DB Table/View Data Preview (multi-tab modal). Does not open until the user clicks Open. Call after the user agrees, or when they explicitly asked to open preview.",
                    Prop("tables", "array", false), Prop("tableName", "string", false),
                    Prop("dataSourceRegisterId", "integer", false), Prop("schemaOwner", "string", false)),
                Tool("list_workspace_files", "List files in the App session workspace (after sync_cloud_artifacts)."),
                Tool("read_workspace_file", "Read a workspace file already pulled into App workspace.", Prop("relativePath", "string", true)),
                Tool("sync_cloud_artifacts",
                    "Pull all Cursor cloud artifacts for this agent into the App session workspace (same mechanism as generated images). "
                    + "After writing deliverables under /opt/cursor/artifacts/ or artifacts/ on the VM, call this once, then list_workspace_files."),
                Tool("write_workspace_file",
                    "DISABLED for file delivery. Write under Cursor artifacts/ then call sync_cloud_artifacts.",
                    Prop("relativePath", "string", true), Prop("content", "string", true)),
                Tool("append_workspace_file",
                    "DISABLED for file delivery. Use Cursor artifacts + sync_cloud_artifacts instead.",
                    Prop("relativePath", "string", true), Prop("content", "string", true)),
                Tool("delete_workspace_file", "Delete a workspace file.", Prop("relativePath", "string", true)),
                Tool("validate_config_pack", "Validate an AppConfigPack JSON file.", Prop("relativePath", "string", true)),
                Tool("preview_config_pack", "Preview import actions without applying.", Prop("relativePath", "string", true)),
                Tool("run_select", "Run a SELECT (row-capped). Use dataSourceRegisterId from list_datasources, or connectionString when the user supplied one.",
                    Prop("sql", "string", true), Prop("dataSourceRegisterId", "integer", false), Prop("connectionString", "string", false)),
                Tool("propose_sql", "Ask the user to confirm INSERT/UPDATE/DELETE/CREATE TABLE/ALTER TABLE ADD.",
                    Prop("sql", "string", true), Prop("dataSourceRegisterId", "integer", false), Prop("connectionString", "string", false))
            };
            var session = AppDataIntegrationAgentContext.Current;
            if (session == null || session.AllowProposeImport)
            {
                tools.Add(Tool("propose_import_pack", "Ask the user to confirm importing a pack. Blocks until they confirm.", Prop("relativePath", "string", true)));
            }
            return tools.ToArray();
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

    public static class AppDataIntegrationAgentContext
    {
        private static readonly AsyncLocal<AppDataIntegrationAgentSessionStore.SessionData> CurrentData
            = new AsyncLocal<AppDataIntegrationAgentSessionStore.SessionData>();

        public static AppDataIntegrationAgentSessionStore.SessionData Current
        {
            get { return CurrentData.Value; }
            set { CurrentData.Value = value; }
        }
    }
}
