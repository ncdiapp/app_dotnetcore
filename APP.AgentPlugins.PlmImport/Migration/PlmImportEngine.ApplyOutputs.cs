using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using App.BL.AIAgent.GenericAgent;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using DatabaseSchemaMrg;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace APP.AgentPlugins.PlmImport
{
    public static partial class PlmImportEngine
    {
        private const string ApplyOutputsStepCode = "import-dw";
        private const string DefaultOutputsContextKey = "plm.integration.import-dw.outputs";
        private const string JobContextKey = "plm.integration.job";

        public static AgentOutputApplyResult ApplyAgentOutputPlan(
            AgentToolContext context,
            string outputsContextKey,
            string planJson,
            int? sessionId,
            int? saasApplicationId,
            string requiredDataSourceIds,
            string modeOverride)
        {
            var result = new AgentOutputApplyResult();
            RequirePlmMigrationAdmin();

            if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                throw new InvalidOperationException("ChatSessionKey is required to apply AgentOutput files.");
            if (context.CompanyId <= 0)
                throw new InvalidOperationException("CompanyId is required to apply AgentOutput files.");

            var key = string.IsNullOrWhiteSpace(outputsContextKey)
                ? DefaultOutputsContextKey
                : outputsContextKey.Trim();

            var outputsRaw = ReadOutputsJson(context, key);
            JObject outputs = null;
            if (!string.IsNullOrWhiteSpace(outputsRaw))
            {
                try { outputs = JObject.Parse(outputsRaw); }
                catch { outputs = null; }
            }

            var planToken = !string.IsNullOrWhiteSpace(planJson)
                ? TryParsePlan(planJson)
                : outputs?["executionPlan"];
            var steps = ParsePlanSteps(planToken);
            result.Planned = steps.Count;
            if (steps.Count == 0)
                throw new InvalidOperationException(
                    "executionPlan is missing or empty. Phase B must write " + key + ".executionPlan.");

            var job = ReadJob(context);
            var resolvedSessionId = sessionId
                ?? job?.Value<int?>("sessionId")
                ?? job?.Value<int?>("SessionId");
            var resolvedSaas = saasApplicationId
                ?? job?.Value<int?>("saasApplicationId")
                ?? job?.Value<int?>("SaasApplicationId");
            var requiredIds = requiredDataSourceIds;
            if (string.IsNullOrWhiteSpace(requiredIds) && job != null)
            {
                var ids = new List<string>();
                AddJobId(ids, job, "plmDataSourceId", "PlmDataSourceId");
                AddJobId(ids, job, "dwDataSourceId", "DwDataSourceId");
                AddJobId(ids, job, "erpDataSourceId", "ErpDataSourceId");
                if (ids.Count > 0)
                    requiredIds = string.Join(",", ids);
            }

            var applyStepCode = ResolveApplyStepCode(key, steps);

            DatabaseFixture logFixture = null;
            try { logFixture = GetTenantFixture(); }
            catch { logFixture = null; }

            var dwDataSourceId = job?.Value<int?>("dwDataSourceId") ?? job?.Value<int?>("DwDataSourceId");

            var missing = new List<string>();
            foreach (var step in steps)
            {
                if (string.IsNullOrWhiteSpace(step.Path))
                    continue;
                var full = GenericAgentFileBL.Resolve(context.ChatSessionKey, step.Path, context.CompanyId);
                if (!File.Exists(full))
                    missing.Add(step.Path.Replace('\\', '/'));
            }
            if (missing.Count > 0)
            {
                result.Ok = false;
                result.Executed = 0;
                var hint = IsImportDwOutputsKey(key)
                    ? " Re-run Phase B so run_agent_script writes 1_PlmDw_Tables.sql, tabs/*/4_TabBlueprint.json, and 4_PlmDw_Assemble.json."
                    : " Re-run Phase B so the child writes the Blueprint JSON under output/{id}/.";
                result.Error = "Agent files not found under AgentOutput/" + context.ChatSessionKey
                    + "/: " + string.Join(", ", missing)
                    + ". executionPlan listing a path is not enough." + hint;
                foreach (var step in steps.OrderBy(s => s.Order))
                {
                    result.Steps.Add(new AgentOutputApplyStepResult
                    {
                        Order = step.Order,
                        Kind = step.Kind,
                        Path = step.Path,
                        Ok = false,
                        Error = missing.Contains((step.Path ?? "").Replace('\\', '/'))
                            ? "Agent file not found."
                            : "Skipped because required output files are missing."
                    });
                }
                result.SessionId = resolvedSessionId;
                result.LogHint = resolvedSessionId.HasValue
                    ? "AppPlmImportLog StepCode=" + applyStepCode + " for sessionId=" + resolvedSessionId.Value
                    : "No sessionId; see AgentOutput apply-log.json and NLog.";
                PersistApplyResult(context, key, outputs, result, steps);
                return result;
            }

            if (IsImportDwOutputsKey(key))
            {
                var planError = ValidateImportDwExecutionPlan(context, steps);
                if (!string.IsNullOrWhiteSpace(planError))
                {
                    result.Ok = false;
                    result.Executed = 0;
                    result.Error = planError;
                    result.SessionId = resolvedSessionId;
                    result.LogHint = "Fix Phase B executionPlan (use 0_ExecutionPlan.suggested.json).";
                    PersistApplyResult(context, key, outputs, result, steps);
                    return result;
                }
            }

            int? templateIdFromPlan = TryParseTemplateIdFromSteps(steps);

            foreach (var step in steps.OrderBy(s => s.Order))
            {
                // BOM / deferred steps: skip when required tab data failed.
                if (step.DependsOnTabIds != null && step.DependsOnTabIds.Count > 0)
                {
                    var failedDeps = step.DependsOnTabIds
                        .Where(tid => !WasTabDataSuccessful(result.Steps, tid))
                        .ToList();
                    if (failedDeps.Count > 0)
                    {
                        var skip = new AgentOutputApplyStepResult
                        {
                            Order = step.Order,
                            Kind = step.Kind,
                            Path = step.Path,
                            Ok = false,
                            ContinueOnError = true,
                            TabId = step.TabId,
                            Error = "Skipped: dependsOnTabIds failed data import: "
                                + string.Join(",", failedDeps.Select(t => "Tab_" + t))
                        };
                        result.Steps.Add(skip);
                        continue;
                    }
                }

                var stepResult = RunOneStep(
                    context, step, requiredIds, resolvedSaas, modeOverride, dwDataSourceId, result.Steps, steps);
                stepResult.ContinueOnError = step.ContinueOnError;
                stepResult.TabId = step.TabId;
                result.Steps.Add(stepResult);

                if (logFixture != null && resolvedSessionId.HasValue && resolvedSessionId.Value > 0)
                {
                    try
                    {
                        WriteImportLog(
                            logFixture,
                            resolvedSessionId.Value,
                            null,
                            applyStepCode,
                            step.Kind + ":" + step.Path,
                            stepResult.Ok ? "Success" : (step.ContinueOnError ? "FailedOptional" : "Failed"),
                            step.Path,
                            step.Kind,
                            stepResult.Batches,
                            stepResult.DurationMs,
                            stepResult.Ok
                                ? (stepResult.Summary ?? "ok")
                                : (stepResult.Error ?? "failed"));
                    }
                    catch
                    {
                        /* logging must not fail the apply */
                    }

                    if (step.TabId.HasValue && templateIdFromPlan.HasValue)
                    {
                        try
                        {
                            string phase = IsBlueprintKind(step.Kind) ? "blueprint" : "data";
                            UpsertDwTabImportStatus(
                                logFixture,
                                resolvedSessionId.Value,
                                templateIdFromPlan.Value,
                                step.TabId.Value,
                                phase,
                                stepResult.Ok ? "Ok" : "Failed",
                                step.Path,
                                stepResult.Ok ? null : stepResult.Error);
                        }
                        catch { /* best-effort */ }
                    }
                }

                if (!stepResult.Ok && !step.ContinueOnError)
                {
                    result.Ok = false;
                    result.Error = "Stopped at order " + step.Order + " (" + step.Path + "): " + stepResult.Error;
                    break;
                }
            }

            var hardFailed = result.Steps.Where(s => !s.Ok && !s.ContinueOnError).ToList();
            var optionalFailed = result.Steps.Where(s => !s.Ok && s.ContinueOnError).ToList();
            result.Executed = result.Steps.Count;

            if (hardFailed.Count > 0)
            {
                result.Ok = false;
                if (string.IsNullOrWhiteSpace(result.Error))
                {
                    var first = hardFailed[0];
                    result.Error = "Stopped at order " + first.Order + " (" + first.Path + "): " + first.Error;
                }
            }
            else if (result.Steps.Count == steps.Count)
            {
                result.Ok = true;
                if (optionalFailed.Count > 0)
                {
                    result.Error = "Partial success: " + optionalFailed.Count + " tab/data step(s) failed (continueOnError). "
                        + string.Join("; ", optionalFailed.Select(s =>
                            (s.TabId.HasValue ? "Tab_" + s.TabId.Value + " " : "") + s.Path + ": " + (s.Error ?? "failed")));
                }
            }
            else
            {
                result.Ok = false;
                if (string.IsNullOrWhiteSpace(result.Error))
                    result.Error = "Apply incomplete: executed " + result.Steps.Count + " of " + steps.Count + " planned steps.";
            }

            result.SessionId = resolvedSessionId;
            result.LogHint = resolvedSessionId.HasValue
                ? "AppPlmImportLog StepCode=" + applyStepCode + " for sessionId=" + resolvedSessionId.Value
                : "No sessionId; see AgentOutput apply-log.json and NLog.";

            PersistApplyResult(context, key, outputs, result, steps);
            TryWriteTabImportStatus(context, steps, result);
            return result;
        }

        private static AgentOutputApplyStepResult RunOneStep(
            AgentToolContext context,
            AgentOutputPlanStep step,
            string requiredDataSourceIds,
            int? saasApplicationId,
            string modeOverride,
            int? dwDataSourceId,
            List<AgentOutputApplyStepResult> priorSteps,
            List<AgentOutputPlanStep> planSteps)
        {
            var stepResult = new AgentOutputApplyStepResult
            {
                Order = step.Order,
                Kind = step.Kind,
                Path = step.Path
            };

            try
            {
                if (string.IsNullOrWhiteSpace(step.Path))
                    throw new InvalidOperationException("executionPlan step is missing path.");

                var kind = (step.Kind ?? "").Trim().ToLowerInvariant();
                if (kind == "sql")
                {
                    AssertImportFromDwReferenceColumn(step.Path, dwDataSourceId);
                    var sql = GenericAgentSqlFileBL.Execute(
                        context.ChatSessionKey,
                        context.CompanyId,
                        step.Path,
                        null,
                        requiredDataSourceIds);
                    stepResult.Ok = sql.Ok;
                    stepResult.Batches = sql.Batches;
                    stepResult.DurationMs = sql.DurationMs;
                    stepResult.Error = sql.Error;
                    stepResult.Summary = sql.Ok
                        ? "batches=" + sql.Batches + " catalog=" + sql.TargetCatalog
                        : null;
                    return stepResult;
                }

                if (kind == "dw-blueprint" || kind == "dw-blueprint-assemble")
                {
                    var mode = !string.IsNullOrWhiteSpace(modeOverride)
                        ? modeOverride.Trim()
                        : (string.IsNullOrWhiteSpace(step.Mode)
                            ? (kind == "dw-blueprint-assemble" ? "Update" : "Insert")
                            : step.Mode.Trim());
                    var json = AgentOutputPathReader.ReadText(context, step.Path);
                    var blueprint = JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(json);
                    if (kind == "dw-blueprint-assemble")
                    {
                        // Merge only tabs whose data AND tab blueprint succeeded. Data-ok but
                        // blueprint-failed packages (e.g. duplicate sibling+grid) must not re-enter Assemble.
                        var okTabs = GetSuccessfulAssembleTabIds(priorSteps);
                        blueprint = MergeAssembleShellWithTabPackages(
                            context, blueprint, okTabs, planSteps, step.Path);
                        blueprint = FilterBlueprintToTabIds(blueprint, okTabs);
                        stepResult.Summary = "assemble tabs=" + string.Join(",", okTabs)
                            + " mergedPackages=" + okTabs.Count;
                    }

                    bool includeSearch = step.IncludeSearchView
                        ?? (kind == "dw-blueprint-assemble" || !step.TabId.HasValue);
                    bool includeNav = step.IncludeNavigation
                        ?? (kind == "dw-blueprint-assemble" || !step.TabId.HasValue);
                    bool includeTg = step.IncludeTransactionGroup
                        ?? (kind == "dw-blueprint-assemble" || !step.TabId.HasValue);

                    var request = new PlmDwBlueprintExecuteRequestDto
                    {
                        Blueprint = blueprint,
                        SaasApplicationId = saasApplicationId,
                        Mode = mode,
                        IncludeSearchView = includeSearch,
                        IncludeNavigation = includeNav,
                        IncludeTransactionGroup = includeTg
                    };
                    var exec = ExecuteDwBlueprintConfig(request);
                    var obj = exec?.Object;
                    stepResult.Ok = obj?.IsSuccess == true;
                    stepResult.DurationMs = 0;
                    stepResult.Error = obj?.ErrorMessage
                        ?? exec?.ValidationResult?.Items?.FirstOrDefault()?.Message;
                    var summaryCore = stepResult.Ok
                        ? "txInserted=" + (obj?.TransactionsInserted ?? 0)
                          + " txUpdated=" + (obj?.TransactionsUpdated ?? 0)
                          + " searchId=" + obj?.SearchId
                        : null;
                    if (!string.IsNullOrWhiteSpace(stepResult.Summary) && summaryCore != null)
                        stepResult.Summary = stepResult.Summary + "; " + summaryCore;
                    else if (summaryCore != null)
                        stepResult.Summary = summaryCore;
                    stepResult.TransactionIds = obj?.TransactionIds;
                    return stepResult;
                }

                if (kind == "search-blueprint")
                {
                    var json = AgentOutputPathReader.ReadText(context, step.Path);
                    var request = new PlmSearchImportExecuteRequestDto
                    {
                        Blueprint = JsonConvert.DeserializeObject<PlmSearchImportBlueprintDto>(json),
                        SaasApplicationId = saasApplicationId
                    };
                    var exec = ExecuteSearchBlueprintConfig(request);
                    var obj = exec?.Object;
                    stepResult.Ok = obj?.IsSuccess == true;
                    stepResult.Error = obj?.ErrorMessage
                        ?? exec?.ValidationResult?.Items?.FirstOrDefault()?.Message;
                    stepResult.Summary = stepResult.Ok
                        ? "searchId=" + obj?.SearchId
                          + " searchViewId=" + obj?.SearchViewId
                          + " dataSetId=" + obj?.DataSetId
                        : null;
                    stepResult.SearchId = obj?.SearchId;
                    stepResult.SearchViewId = obj?.SearchViewId;
                    return stepResult;
                }

                if (kind == "search-additional-view" || kind == "search-sibling-view")
                {
                    var json = AgentOutputPathReader.ReadText(context, step.Path);
                    var request = new PlmSearchSiblingViewExecuteRequestDto
                    {
                        Blueprint = JsonConvert.DeserializeObject<PlmSearchSiblingViewBlueprintDto>(json),
                        SaasApplicationId = saasApplicationId
                    };
                    var exec = ExecuteSearchSiblingViewConfig(request);
                    var obj = exec?.Object;
                    stepResult.Ok = obj?.IsSuccess == true;
                    stepResult.Error = obj?.ErrorMessage
                        ?? exec?.ValidationResult?.Items?.FirstOrDefault()?.Message;
                    stepResult.Summary = stepResult.Ok
                        ? "searchId=" + obj?.SearchId
                          + " searchViewId=" + obj?.SiblingSearchViewId
                          + " dataSetId=" + obj?.DataSetId
                        : null;
                    stepResult.SearchId = obj?.SearchId;
                    stepResult.SearchViewId = obj?.SiblingSearchViewId;
                    return stepResult;
                }

                if (kind == "search-massupdate")
                {
                    var json = AgentOutputPathReader.ReadText(context, step.Path);
                    var request = new PlmSearchMassUpdateViewExecuteRequestDto
                    {
                        Blueprint = JsonConvert.DeserializeObject<PlmSearchMassUpdateViewBlueprintDto>(json),
                        SaasApplicationId = saasApplicationId
                    };
                    var exec = ExecuteSearchMassUpdateViewConfig(request);
                    var obj = exec?.Object;
                    stepResult.Ok = obj?.IsSuccess == true;
                    stepResult.Error = obj?.ErrorMessage
                        ?? exec?.ValidationResult?.Items?.FirstOrDefault()?.Message;
                    stepResult.Summary = stepResult.Ok
                        ? "searchId=" + obj?.SearchId
                          + " massUpdateViewId=" + obj?.MassUpdateSearchViewId
                          + " listEditTx=" + obj?.ListEditTransactionId
                        : null;
                    stepResult.SearchId = obj?.SearchId;
                    stepResult.SearchViewId = obj?.MassUpdateSearchViewId;
                    return stepResult;
                }

                throw new InvalidOperationException("Unsupported executionPlan kind '" + step.Kind + "'.");
            }
            catch (Exception ex)
            {
                stepResult.Ok = false;
                stepResult.Error = ex.Message;
                return stepResult;
            }
        }

        /// <summary>
        /// Step 3 INSERT uses src.[FieldMapping.DwColumnName]. The APP column is always
        /// ReferenceCode; DwColumnName must be the physical DW column (Article__22, etc.).
        /// </summary>
        private static void AssertImportFromDwReferenceColumn(string path, int? dwDataSourceId)
        {
            if (string.IsNullOrWhiteSpace(path) || !dwDataSourceId.HasValue || dwDataSourceId.Value <= 0)
                return;

            var file = path.Replace('\\', '/');
            var slash = file.LastIndexOf('/');
            var name = slash >= 0 ? file.Substring(slash + 1) : file;
            if (!name.Equals("3_PlmDw_ImportFromDW.sql", StringComparison.OrdinalIgnoreCase))
                return;

            string dwTable = null;
            string dwColumn = null;
            using (var conn = new SqlConnection(GetTenantConnectionString()))
            {
                conn.Open();
                if (!TemplateTableExists(conn, null, "Plm_FieldMapping"))
                    return;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TOP 1 DwTableName, DwColumnName
FROM dbo.Plm_FieldMapping
WHERE AppColumnName = N'ReferenceCode'
  AND FieldKind = N'ReferenceField'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return;
                        dwTable = reader.IsDBNull(0) ? null : reader.GetString(0);
                        dwColumn = reader.IsDBNull(1) ? null : reader.GetString(1);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(dwTable) || string.IsNullOrWhiteSpace(dwColumn))
                return;

            var hints = new List<string>();
            var exists = false;
            using (var conn = new SqlConnection(ResolveConnectionStringFromRegisterId(dwDataSourceId.Value)))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT c.name
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name = @table
  AND c.name NOT IN (N'TabID', N'ProductReferenceID')
ORDER BY c.name";
                    cmd.Parameters.AddWithValue("@table", dwTable);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var col = reader.GetString(0);
                            if (string.Equals(col, dwColumn, StringComparison.OrdinalIgnoreCase))
                                exists = true;
                            if (hints.Count < 12
                                && (col.IndexOf("Article", StringComparison.OrdinalIgnoreCase) >= 0
                                    || col.IndexOf("Code", StringComparison.OrdinalIgnoreCase) >= 0
                                    || col.IndexOf("Name_", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                hints.Add(col);
                            }
                        }
                    }
                }
            }

            if (exists)
                return;

            var hintText = hints.Count > 0 ? string.Join(", ", hints) : "(none)";
            throw new InvalidOperationException(
                "ReferenceField DwColumnName [" + dwColumn + "] does not exist on dbo." + dwTable
                + ". DwColumnName must be the physical DW column (e.g. Article__22), not the APP name ReferenceCode. Candidates: "
                + hintText);
        }

        private static void PersistApplyResult(
            AgentToolContext context,
            string outputsKey,
            JObject outputs,
            AgentOutputApplyResult result,
            List<AgentOutputPlanStep> planned)
        {
            var applyJson = JsonConvert.SerializeObject(result);
            var root = outputs ?? new JObject();
            root["apply"] = JToken.Parse(applyJson);
            WriteOutputsJson(context, outputsKey, root.ToString(Formatting.None));

            var folder = planned
                .Select(s => s.Path)
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            if (string.IsNullOrWhiteSpace(folder))
                return;
            var dir = folder.Replace('\\', '/');
            var slash = dir.LastIndexOf('/');
            var logPath = (slash >= 0 ? dir.Substring(0, slash) : "output") + "/apply-log.json";
            try
            {
                GenericAgentFileBL.WriteText(context.ChatSessionKey, logPath, applyJson, context.CompanyId);
                result.LogFile = logPath;
            }
            catch
            {
                /* apply-log.json is best-effort */
            }
        }

        private static string ReadOutputsJson(AgentToolContext context, string key)
        {
            var raw = AppAgentSharedContextBL.ReadContext(context.WorkflowId, key, context.DataSourceId);
            if (!string.IsNullOrWhiteSpace(raw))
                return raw;
            if (!string.IsNullOrWhiteSpace(context.ChatSessionKey)
                && !string.Equals(context.ChatSessionKey, context.WorkflowId, StringComparison.OrdinalIgnoreCase))
            {
                return AppAgentSharedContextBL.ReadContext(context.ChatSessionKey, key, context.DataSourceId);
            }
            return null;
        }

        private static void WriteOutputsJson(AgentToolContext context, string key, string json)
        {
            AppAgentSharedContextBL.WriteContext(context.WorkflowId, key, json, context.DataSourceId);
            if (!string.IsNullOrWhiteSpace(context.ChatSessionKey)
                && !string.Equals(context.ChatSessionKey, context.WorkflowId, StringComparison.OrdinalIgnoreCase))
            {
                AppAgentSharedContextBL.WriteContext(context.ChatSessionKey, key, json, context.DataSourceId);
            }
        }

        private static JObject ReadJob(AgentToolContext context)
        {
            var raw = AppAgentSharedContextBL.ReadContext(context.WorkflowId, JobContextKey, context.DataSourceId);
            if (string.IsNullOrWhiteSpace(raw)
                && !string.IsNullOrWhiteSpace(context.ChatSessionKey)
                && !string.Equals(context.ChatSessionKey, context.WorkflowId, StringComparison.OrdinalIgnoreCase))
            {
                raw = AppAgentSharedContextBL.ReadContext(context.ChatSessionKey, JobContextKey, context.DataSourceId);
            }
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            try { return JObject.Parse(raw); }
            catch { return null; }
        }

        private static void AddJobId(List<string> ids, JObject job, string camel, string pascal)
        {
            var n = job.Value<int?>(camel) ?? job.Value<int?>(pascal);
            if (n.HasValue && n.Value > 0)
                ids.Add(n.Value.ToString());
        }

        private static bool IsImportDwOutputsKey(string key)
        {
            return string.IsNullOrWhiteSpace(key)
                || key.IndexOf("import-dw", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveApplyStepCode(string outputsKey, List<AgentOutputPlanStep> steps)
        {
            if (!string.IsNullOrWhiteSpace(outputsKey))
            {
                if (outputsKey.IndexOf("massupdate", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "massupdate";
                if (outputsKey.IndexOf(".search", StringComparison.OrdinalIgnoreCase) >= 0
                    || outputsKey.EndsWith("search.outputs", StringComparison.OrdinalIgnoreCase))
                    return "search";
                if (outputsKey.IndexOf("import-dw", StringComparison.OrdinalIgnoreCase) >= 0)
                    return ApplyOutputsStepCode;
            }

            var firstKind = (steps != null && steps.Count > 0 ? steps[0].Kind : null) ?? "";
            firstKind = firstKind.Trim().ToLowerInvariant();
            if (firstKind == "search-massupdate")
                return "massupdate";
            if (firstKind.StartsWith("search-", StringComparison.Ordinal))
                return "search";
            return ApplyOutputsStepCode;
        }

        private static JToken TryParsePlan(string planJson)
        {
            try
            {
                var token = JToken.Parse(planJson);
                if (token is JObject obj && obj["executionPlan"] != null)
                    return obj["executionPlan"];
                return token;
            }
            catch
            {
                return null;
            }
        }

        private static List<AgentOutputPlanStep> ParsePlanSteps(JToken planToken)
        {
            var list = new List<AgentOutputPlanStep>();
            if (planToken is not JArray arr)
                return list;
            foreach (var item in arr.OfType<JObject>())
            {
                var depends = new List<int>();
                var depToken = item["dependsOnTabIds"] ?? item["DependsOnTabIds"];
                if (depToken is JArray depArr)
                {
                    foreach (var d in depArr)
                    {
                        if (d != null && int.TryParse(d.ToString(), out int tid) && tid > 0)
                            depends.Add(tid);
                    }
                }

                list.Add(new AgentOutputPlanStep
                {
                    Order = item.Value<int?>("order") ?? item.Value<int?>("Order") ?? 0,
                    Kind = item.Value<string>("kind") ?? item.Value<string>("Kind"),
                    Path = item.Value<string>("path") ?? item.Value<string>("Path"),
                    Target = item.Value<string>("target") ?? item.Value<string>("Target"),
                    Mode = item.Value<string>("mode") ?? item.Value<string>("Mode"),
                    Label = item.Value<string>("label") ?? item.Value<string>("Label"),
                    TabId = item.Value<int?>("tabId") ?? item.Value<int?>("TabId"),
                    ContinueOnError = item.Value<bool?>("continueOnError")
                        ?? item.Value<bool?>("ContinueOnError")
                        ?? false,
                    IncludeSearchView = item.Value<bool?>("includeSearchView")
                        ?? item.Value<bool?>("IncludeSearchView"),
                    IncludeNavigation = item.Value<bool?>("includeNavigation")
                        ?? item.Value<bool?>("IncludeNavigation"),
                    IncludeTransactionGroup = item.Value<bool?>("includeTransactionGroup")
                        ?? item.Value<bool?>("IncludeTransactionGroup"),
                    DependsOnTabIds = depends
                });
            }
            return list;
        }

        private static bool IsBlueprintKind(string kind)
        {
            var k = (kind ?? "").Trim().ToLowerInvariant();
            return k == "dw-blueprint" || k == "dw-blueprint-assemble";
        }

        private static int? TryParseTemplateIdFromSteps(List<AgentOutputPlanStep> steps)
        {
            foreach (var s in steps ?? Enumerable.Empty<AgentOutputPlanStep>())
            {
                var p = (s.Path ?? "").Replace('\\', '/');
                var m = System.Text.RegularExpressions.Regex.Match(p, @"^output/(\d+)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int id))
                    return id;
            }
            return null;
        }

        private static string ValidateImportDwExecutionPlan(AgentToolContext context, List<AgentOutputPlanStep> steps)
        {
            if (steps == null || steps.Count == 0)
                return "executionPlan is empty.";

            bool hasPerTabData = steps.Any(s =>
                (s.Path ?? "").Replace('\\', '/').IndexOf("/tabs/", StringComparison.OrdinalIgnoreCase) >= 0
                && (s.Path ?? "").EndsWith("3_ImportFromDW.sql", StringComparison.OrdinalIgnoreCase));

            bool hasRoot = steps.Any(s =>
                (s.Path ?? "").Replace('\\', '/').EndsWith("3_00_Root_ImportFromDW.sql", StringComparison.OrdinalIgnoreCase));

            bool hasMonolith = steps.Any(s =>
            {
                var p = (s.Path ?? "").Replace('\\', '/');
                return p.EndsWith("3_PlmDw_ImportFromDW.sql", StringComparison.OrdinalIgnoreCase)
                    && p.IndexOf("/tabs/", StringComparison.OrdinalIgnoreCase) < 0
                    && p.IndexOf("3_00_Root", StringComparison.OrdinalIgnoreCase) < 0;
            });

            // If AgentOutput has any tabs/*/3_ package, plan must use per-tab path (not monolith).
            string templateFolder = null;
            foreach (var s in steps)
            {
                var p = (s.Path ?? "").Replace('\\', '/');
                var m = System.Text.RegularExpressions.Regex.Match(p, @"^output/(\d+)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success) { templateFolder = m.Groups[1].Value; break; }
            }
            if (!string.IsNullOrWhiteSpace(templateFolder))
            {
                var tabsDir = GenericAgentFileBL.Resolve(
                    context.ChatSessionKey, "output/" + templateFolder + "/tabs", context.CompanyId);
                bool diskHasPerTab = Directory.Exists(tabsDir)
                    && Directory.GetDirectories(tabsDir).Any(d =>
                        File.Exists(Path.Combine(d, "3_ImportFromDW.sql")));
                if (diskHasPerTab)
                {
                    if (hasMonolith)
                        return "Invalid executionPlan: per-tab packages exist under output/"
                            + templateFolder + "/tabs but plan still includes monolith 3_PlmDw_ImportFromDW.sql. "
                            + "Copy output/" + templateFolder + "/0_ExecutionPlan.suggested.json instead.";
                    if (!hasRoot)
                        return "Invalid executionPlan: missing 3_00_Root_ImportFromDW.sql (required before per-tab data).";
                    if (!hasPerTabData)
                        return "Invalid executionPlan: output/" + templateFolder
                            + "/tabs/*/3_ImportFromDW.sql exist but none are listed. Use 0_ExecutionPlan.suggested.json.";
                }
            }

            return null;
        }

        private static bool WasTabDataSuccessful(List<AgentOutputApplyStepResult> prior, int tabId)
        {
            var dataSteps = (prior ?? new List<AgentOutputApplyStepResult>())
                .Where(s => s.TabId == tabId
                    && !IsBlueprintKind(s.Kind)
                    && (s.Path ?? "").IndexOf("/tabs/", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (dataSteps.Count == 0)
                return true; // no data step for this tab in plan — don't block
            return dataSteps.Any(s => s.Ok);
        }

        private static List<int> GetSuccessfulDataTabIds(List<AgentOutputApplyStepResult> prior)
        {
            return (prior ?? new List<AgentOutputApplyStepResult>())
                .Where(s => s.Ok && s.TabId.HasValue
                    && !IsBlueprintKind(s.Kind)
                    && (s.Path ?? "").IndexOf("3_ImportFromDW.sql", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(s => s.TabId.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        /// <summary>
        /// Tabs eligible for Assemble merge: data import succeeded and tab blueprint did not fail.
        /// </summary>
        private static List<int> GetSuccessfulAssembleTabIds(List<AgentOutputApplyStepResult> prior)
        {
            var dataOk = GetSuccessfulDataTabIds(prior);
            if (dataOk.Count == 0)
                return dataOk;

            var blueprintFailed = new HashSet<int>(
                (prior ?? new List<AgentOutputApplyStepResult>())
                    .Where(s => s != null
                        && !s.Ok
                        && s.TabId.HasValue
                        && IsBlueprintKind(s.Kind)
                        && (s.Path ?? "").IndexOf("4_TabBlueprint", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(s => s.TabId.Value));

            return dataOk.Where(id => !blueprintFailed.Contains(id)).ToList();
        }

        /// <summary>
        /// Assemble shell (TG/Search/Nav + plmTabId&lt;=0) + successful tabs' 4_TabBlueprint packages.
        /// </summary>
        private static PlmDwImportBlueprintDto MergeAssembleShellWithTabPackages(
            AgentToolContext context,
            PlmDwImportBlueprintDto shell,
            List<int> okTabIds,
            List<AgentOutputPlanStep> planSteps,
            string assemblePath)
        {
            if (shell == null)
                return null;

            shell.Transactions = shell.Transactions ?? new List<PlmDwBlueprintTransactionDto>();
            shell.GridBindings = shell.GridBindings ?? new List<PlmDwBlueprintGridBindingDto>();
            shell.BlueprintFields = shell.BlueprintFields ?? new List<PlmDwBlueprintFieldDto>();
            shell.BomColorwayPivotBindings = shell.BomColorwayPivotBindings
                ?? new List<PlmDwBlueprintBomColorwayPivotBindingDto>();
            shell.TechPackGradeValuePivotBindings = shell.TechPackGradeValuePivotBindings
                ?? new List<PlmDwBlueprintTechPackGradeValuePivotDto>();
            shell.TechPackFitMeasurementPivotBindings = shell.TechPackFitMeasurementPivotBindings
                ?? new List<PlmDwBlueprintTechPackFitMeasurementPivotDto>();
            shell.TechPackSimpleQcPivotBindings = shell.TechPackSimpleQcPivotBindings
                ?? new List<PlmDwBlueprintTechPackSimpleQcPivotDto>();
            shell.TabSharedTableGroups = shell.TabSharedTableGroups
                ?? new List<PlmDwBlueprintTabSharedTableGroupDto>();

            string templateFolder = null;
            var assembleNorm = (assemblePath ?? "").Replace('\\', '/');
            var am = System.Text.RegularExpressions.Regex.Match(
                assembleNorm, @"^output/(\d+)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (am.Success)
                templateFolder = am.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(templateFolder))
            {
                foreach (var s in planSteps ?? new List<AgentOutputPlanStep>())
                {
                    var p = (s.Path ?? "").Replace('\\', '/');
                    var m = System.Text.RegularExpressions.Regex.Match(
                        p, @"^output/(\d+)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success) { templateFolder = m.Groups[1].Value; break; }
                }
            }

            var txIds = new HashSet<string>(
                shell.Transactions
                    .Where(t => t != null && !string.IsNullOrWhiteSpace(t.IntegrationId))
                    .Select(t => t.IntegrationId),
                StringComparer.OrdinalIgnoreCase);

            foreach (var tabId in okTabIds ?? new List<int>())
            {
                string tabPath = null;
                var planHit = (planSteps ?? new List<AgentOutputPlanStep>()).FirstOrDefault(s =>
                    s.TabId == tabId
                    && (s.Path ?? "").Replace('\\', '/').EndsWith(
                        "4_TabBlueprint.json", StringComparison.OrdinalIgnoreCase));
                if (planHit != null)
                    tabPath = planHit.Path;
                else if (!string.IsNullOrWhiteSpace(templateFolder))
                    tabPath = "output/" + templateFolder + "/tabs/" + tabId + "/4_TabBlueprint.json";

                if (string.IsNullOrWhiteSpace(tabPath))
                    continue;

                string tabJson;
                try
                {
                    tabJson = AgentOutputPathReader.ReadText(context, tabPath);
                }
                catch
                {
                    continue;
                }

                var tabBp = JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(tabJson);
                if (tabBp == null)
                    continue;

                foreach (var t in tabBp.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
                {
                    if (t == null) continue;
                    if (!string.IsNullOrWhiteSpace(t.IntegrationId) && !txIds.Add(t.IntegrationId))
                        continue;
                    shell.Transactions.Add(t);
                }

                foreach (var g in tabBp.GridBindings ?? Enumerable.Empty<PlmDwBlueprintGridBindingDto>())
                {
                    if (g != null)
                        shell.GridBindings.Add(g);
                }

                foreach (var f in tabBp.BlueprintFields ?? Enumerable.Empty<PlmDwBlueprintFieldDto>())
                {
                    if (f != null)
                        shell.BlueprintFields.Add(f);
                }

                foreach (var b in tabBp.BomColorwayPivotBindings
                             ?? Enumerable.Empty<PlmDwBlueprintBomColorwayPivotBindingDto>())
                {
                    if (b != null)
                        shell.BomColorwayPivotBindings.Add(b);
                }

                foreach (var b in tabBp.TechPackGradeValuePivotBindings
                             ?? Enumerable.Empty<PlmDwBlueprintTechPackGradeValuePivotDto>())
                {
                    if (b != null)
                        shell.TechPackGradeValuePivotBindings.Add(b);
                }

                foreach (var b in tabBp.TechPackFitMeasurementPivotBindings
                             ?? Enumerable.Empty<PlmDwBlueprintTechPackFitMeasurementPivotDto>())
                {
                    if (b != null)
                        shell.TechPackFitMeasurementPivotBindings.Add(b);
                }

                foreach (var b in tabBp.TechPackSimpleQcPivotBindings
                             ?? Enumerable.Empty<PlmDwBlueprintTechPackSimpleQcPivotDto>())
                {
                    if (b != null)
                        shell.TechPackSimpleQcPivotBindings.Add(b);
                }

                foreach (var g in tabBp.TabSharedTableGroups
                             ?? Enumerable.Empty<PlmDwBlueprintTabSharedTableGroupDto>())
                {
                    if (g == null) continue;
                    bool exists = shell.TabSharedTableGroups.Any(x =>
                        x != null
                        && x.PrimaryPlmTabId == g.PrimaryPlmTabId
                        && string.Equals(x.SharedAppTableName, g.SharedAppTableName, StringComparison.OrdinalIgnoreCase));
                    if (!exists)
                        shell.TabSharedTableGroups.Add(g);
                }
            }

            if (shell.Source == null)
                shell.Source = new PlmDwBlueprintSourceDto();
            shell.Source.ImportTabIds = (okTabIds ?? new List<int>()).OrderBy(x => x).ToList();

            return shell;
        }

        private static PlmDwImportBlueprintDto FilterBlueprintToTabIds(
            PlmDwImportBlueprintDto blueprint,
            List<int> okTabIds)
        {
            if (blueprint == null)
                return null;
            var ok = new HashSet<int>(okTabIds ?? new List<int>());
            // Always keep FitRound-style txs with plmTabId=0 if present when any tab ok; drop normal tabs not in ok.
            blueprint.Transactions = (blueprint.Transactions ?? new List<PlmDwBlueprintTransactionDto>())
                .Where(t => t != null && (t.PlmTabId <= 0 || ok.Contains(t.PlmTabId)))
                .ToList();
            var keepIntegration = new HashSet<string>(
                blueprint.Transactions.Select(t => t.IntegrationId).Where(s => !string.IsNullOrWhiteSpace(s)),
                StringComparer.OrdinalIgnoreCase);

            blueprint.GridBindings = (blueprint.GridBindings ?? new List<PlmDwBlueprintGridBindingDto>())
                .Where(g => g != null && (
                    (g.ParentPlmTabId.HasValue && ok.Contains(g.ParentPlmTabId.Value))
                    || (!string.IsNullOrWhiteSpace(g.TransactionIntegrationId)
                        && keepIntegration.Contains(g.TransactionIntegrationId))))
                .ToList();

            if (blueprint.Source != null)
                blueprint.Source.ImportTabIds = ok.OrderBy(x => x).ToList();

            blueprint.BomColorwayPivotBindings = (blueprint.BomColorwayPivotBindings
                    ?? new List<PlmDwBlueprintBomColorwayPivotBindingDto>())
                .Where(b => b != null && ok.Contains(b.PlmTabId))
                .ToList();

            return blueprint;
        }

        private static void TryWriteTabImportStatus(
            AgentToolContext context,
            List<AgentOutputPlanStep> steps,
            AgentOutputApplyResult result)
        {
            try
            {
                var tabSteps = steps
                    .Where(s => s.TabId.HasValue || (s.Path != null && s.Path.IndexOf("/tabs/", StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
                if (tabSteps.Count == 0)
                    return;

                string templateFolder = null;
                foreach (var s in steps)
                {
                    var p = (s.Path ?? "").Replace('\\', '/');
                    var m = System.Text.RegularExpressions.Regex.Match(p, @"^output/(\d+)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        templateFolder = m.Groups[1].Value;
                        break;
                    }
                }
                if (string.IsNullOrWhiteSpace(templateFolder))
                    return;

                var rows = new List<object>();
                foreach (var plan in tabSteps)
                {
                    var sr = result.Steps.FirstOrDefault(x => x.Order == plan.Order);
                    rows.Add(new
                    {
                        tabId = plan.TabId,
                        path = plan.Path,
                        label = plan.Label,
                        ok = sr?.Ok == true,
                        error = sr?.Error,
                        continueOnError = plan.ContinueOnError
                    });
                }

                var payload = new
                {
                    generatedAt = DateTime.UtcNow.ToString("o"),
                    applyOk = result.Ok,
                    applyError = result.Error,
                    tabs = rows
                };
                var rel = "output/" + templateFolder + "/tab-import-status.json";
                var full = GenericAgentFileBL.Resolve(context.ChatSessionKey, rel, context.CompanyId);
                Directory.CreateDirectory(Path.GetDirectoryName(full) ?? full);
                File.WriteAllText(full, JsonConvert.SerializeObject(payload, Formatting.Indented));
            }
            catch
            {
                /* status file is best-effort */
            }
        }
    }

    public sealed class AgentOutputPlanStep
    {
        public int Order { get; set; }
        public string Kind { get; set; }
        public string Path { get; set; }
        public string Target { get; set; }
        public string Mode { get; set; }
        public string Label { get; set; }
        public int? TabId { get; set; }
        public bool ContinueOnError { get; set; }
        public bool? IncludeSearchView { get; set; }
        public bool? IncludeNavigation { get; set; }
        public bool? IncludeTransactionGroup { get; set; }
        public List<int> DependsOnTabIds { get; set; } = new List<int>();
    }

    public sealed class AgentOutputApplyResult
    {
        public bool Ok { get; set; }
        public int Planned { get; set; }
        public int Executed { get; set; }
        public string Error { get; set; }
        public int? SessionId { get; set; }
        public string LogHint { get; set; }
        public string LogFile { get; set; }
        public List<AgentOutputApplyStepResult> Steps { get; set; } = new List<AgentOutputApplyStepResult>();
    }

    public sealed class AgentOutputApplyStepResult
    {
        public int Order { get; set; }
        public string Kind { get; set; }
        public string Path { get; set; }
        public bool Ok { get; set; }
        public int Batches { get; set; }
        public int DurationMs { get; set; }
        public string Error { get; set; }
        public string Summary { get; set; }
        public List<int> TransactionIds { get; set; }
        public int? SearchId { get; set; }
        public int? SearchViewId { get; set; }
        public int? TabId { get; set; }
        public bool ContinueOnError { get; set; }
    }
}
