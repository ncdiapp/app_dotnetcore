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
                    "executionPlan is missing or empty. Phase B must write plm.integration.import-dw.outputs.executionPlan.");

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
                result.Error = "Agent files not found under AgentOutput/" + context.ChatSessionKey
                    + "/: " + string.Join(", ", missing)
                    + ". executionPlan listing a path is not enough. Re-run Phase B so run_agent_script writes 1_PlmDw_Tables.sql and 4_PlmDw_ImportBlueprint.json.";
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
                    ? "AppPlmImportLog StepCode=import-dw for sessionId=" + resolvedSessionId.Value
                    : "No sessionId; see AgentOutput apply-log.json and NLog.";
                PersistApplyResult(context, key, outputs, result, steps);
                return result;
            }

            foreach (var step in steps.OrderBy(s => s.Order))
            {
                var stepResult = RunOneStep(
                    context, step, requiredIds, resolvedSaas, modeOverride, dwDataSourceId);
                result.Steps.Add(stepResult);

                if (logFixture != null && resolvedSessionId.HasValue && resolvedSessionId.Value > 0)
                {
                    try
                    {
                        WriteImportLog(
                            logFixture,
                            resolvedSessionId.Value,
                            null,
                            ApplyOutputsStepCode,
                            step.Kind + ":" + step.Path,
                            stepResult.Ok ? "Success" : "Failed",
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
                }

                if (!stepResult.Ok)
                {
                    result.Ok = false;
                    result.Error = "Stopped at order " + step.Order + " (" + step.Path + "): " + stepResult.Error;
                    break;
                }
            }

            if (result.Steps.Count == steps.Count && result.Steps.All(s => s.Ok))
                result.Ok = true;

            result.Executed = result.Steps.Count(s => s.Ok);
            result.SessionId = resolvedSessionId;
            result.LogHint = resolvedSessionId.HasValue
                ? "AppPlmImportLog StepCode=import-dw for sessionId=" + resolvedSessionId.Value
                : "No sessionId; see AgentOutput apply-log.json and NLog.";

            PersistApplyResult(context, key, outputs, result, steps);
            return result;
        }

        private static AgentOutputApplyStepResult RunOneStep(
            AgentToolContext context,
            AgentOutputPlanStep step,
            string requiredDataSourceIds,
            int? saasApplicationId,
            string modeOverride,
            int? dwDataSourceId)
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

                if (kind == "dw-blueprint")
                {
                    var mode = !string.IsNullOrWhiteSpace(modeOverride)
                        ? modeOverride.Trim()
                        : (string.IsNullOrWhiteSpace(step.Mode) ? "Insert" : step.Mode.Trim());
                    var json = AgentOutputPathReader.ReadText(context, step.Path);
                    var request = new PlmDwBlueprintExecuteRequestDto
                    {
                        Blueprint = JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(json),
                        SaasApplicationId = saasApplicationId,
                        Mode = mode,
                        IncludeSearchView = true,
                        IncludeNavigation = true,
                        IncludeTransactionGroup = true
                    };
                    var exec = ExecuteDwBlueprintConfig(request);
                    var obj = exec?.Object;
                    stepResult.Ok = obj?.IsSuccess == true;
                    stepResult.DurationMs = 0;
                    stepResult.Error = obj?.ErrorMessage
                        ?? exec?.ValidationResult?.Items?.FirstOrDefault()?.Message;
                    stepResult.Summary = stepResult.Ok
                        ? "txInserted=" + (obj?.TransactionsInserted ?? 0)
                          + " txUpdated=" + (obj?.TransactionsUpdated ?? 0)
                          + " searchId=" + obj?.SearchId
                        : null;
                    stepResult.TransactionIds = obj?.TransactionIds;
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
                list.Add(new AgentOutputPlanStep
                {
                    Order = item.Value<int?>("order") ?? item.Value<int?>("Order") ?? 0,
                    Kind = item.Value<string>("kind") ?? item.Value<string>("Kind"),
                    Path = item.Value<string>("path") ?? item.Value<string>("Path"),
                    Target = item.Value<string>("target") ?? item.Value<string>("Target"),
                    Mode = item.Value<string>("mode") ?? item.Value<string>("Mode"),
                    Label = item.Value<string>("label") ?? item.Value<string>("Label")
                });
            }
            return list;
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
    }
}
