using System;
using System.Collections.Generic;
using System.Linq;
using App.BL.AIAgent.GenericAgent;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using App.BL.DbGenie;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    public class DataQueryPlugin
    {
        private readonly int? _dataSourceId;

        public DataQueryPlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("execute_sql",
            "Execute a SQL SELECT query. Only SELECT is allowed. Optional dataSourceId targets a registered DataSource; omit for session default. "
            + "For unbounded detail SELECTs the platform COUNTs first: ≤250 rows → fetch all unless user asked for N or SQL has hard TOP N; larger → ask_user confirm_row_limit. "
            + "When the user wants N rows, use SELECT TOP N or rely on platform turn intent; pass render_interactive limit=N. "
            + "Scalar COUNT(*) / GROUP BY may omit TOP. "
            + "Platform caches tools up to MaxCachedRows; LLM only sees ~20 sample rows.")]
        public string ExecuteSql(
            [AgentParam("A SQL SELECT statement. Must start with SELECT.", isRequired: true)] string sql,
            [AgentParam("Optional DataSourceRegisterId. When omitted or 0, uses the session default data source.")] int? dataSourceId = null,
            AgentToolContext context = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sql))
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "sql parameter is required. Pass a SELECT statement." });

                if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = "Only SELECT statements are allowed. Use create_database_table for DDL."
                    });

                var sessionKey = context?.ChatSessionKey;
                var hasChoice = AgentRowLimitConfirmBL.TryGetMaxRows(sessionKey, out var chosenMax);
                var deterministic = context != null && context.IsDeterministic;
                var ds = ResolveDataSourceId(dataSourceId);
                var needsGate = AgentRowLimitConfirmBL.NeedsRowLimitConfirm(sql);
                var onAskBound = AgentHitlBridge.Resolve(sessionKey)?.OnAskUser != null;
                var log = NLog.LogManager.GetCurrentClassLogger();
                log.Info(
                    "[RowLimit] enter session={0} hasChoice={1} chosenMax={2} needsGate={3} deterministic={4} onAskBound={5} sqlPreview={6}",
                    sessionKey ?? "(null)",
                    hasChoice,
                    hasChoice ? chosenMax : 0,
                    needsGate,
                    deterministic,
                    onAskBound,
                    (sql ?? "").Length > 180 ? (sql ?? "").Substring(0, 180) + "…" : (sql ?? ""));

                int rowCap;
                bool forceTop;
                long? countedRows = null;
                string gatePath = "unknown";
                int? displayRowLimit = null;

                // Strip soft preview TOP before fetch when we will apply platform cap / user choice.
                var sqlToRun = sql;

                if (hasChoice)
                {
                    rowCap = chosenMax;
                    displayRowLimit = rowCap;
                    forceTop = true;
                    sqlToRun = AgentRowLimitConfirmBL.StripTopOrLimit(sql);
                    gatePath = "hasChoice";
                }
                else if (needsGate)
                {
                    // COUNT first — only ask when the real total is large.
                    var countSql = AgentRowLimitConfirmBL.BuildCountSql(sql);
                    // SkipApplyQueryLimits: MaxRows/TOP rewrite must not touch the inner SELECT
                    // (old MaxRows=1 rewrote every SELECT → COUNT of TOP 1 = 1 → autoShowAll, no ask).
                    var countResult = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                    {
                        SQL                   = countSql,
                        DataSourceRegisterId  = ds,
                        RequireConfirmation   = false,
                        IsConfirmed           = true,
                        SkipApplyQueryLimits  = true
                    });
                    if (!countResult.IsSuccess)
                    {
                        log.Warn("[RowLimit] COUNT failed session={0} err={1}", sessionKey ?? "(null)", countResult.Error);
                        return JsonConvert.SerializeObject(new { countResult.IsSuccess, countResult.Error, step = "count" });
                    }

                    if (!AgentRowLimitConfirmBL.TryParseCount(countResult.Results, out var totalCount))
                    {
                        log.Warn("[RowLimit] COUNT parse failed session={0}", sessionKey ?? "(null)");
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            Error = "Could not parse COUNT result for row-limit gate."
                        });
                    }

                    countedRows = totalCount;
                    if (!string.IsNullOrWhiteSpace(sessionKey))
                        AgentRowLimitConfirmBL.RememberCount(sessionKey, totalCount);

                    log.Info("[RowLimit] COUNT={0} session={1}", totalCount, sessionKey ?? "(null)");

                    if (AgentRowLimitConfirmBL.ShouldAutoShowAll(totalCount))
                    {
                        if (AgentRowLimitConfirmBL.TryGetHardTopRowLimit(sql, out var hardTop))
                        {
                            rowCap = (int)Math.Min(hardTop, totalCount);
                            displayRowLimit = rowCap;
                            gatePath = "autoShowHardTop";
                        }
                        else if (AgentDisplayRowLimitBL.TryGetTurnRowIntent(sessionKey, out var turnN))
                        {
                            rowCap = (int)Math.Min(turnN, totalCount);
                            displayRowLimit = rowCap;
                            gatePath = "autoShowTurnIntent";
                        }
                        else
                        {
                            rowCap = AgentDataAnalyzeCacheBL.MaxCachedRows;
                            gatePath = "autoShowAll";
                        }
                        forceTop = true;
                        sqlToRun = AgentRowLimitConfirmBL.StripTopOrLimit(sql);
                    }
                    else if (deterministic)
                    {
                        rowCap = 1000;
                        displayRowLimit = 1000;
                        forceTop = true;
                        sqlToRun = AgentRowLimitConfirmBL.StripTopOrLimit(sql);
                        gatePath = "deterministicCap1000";
                    }
                    else
                    {
                        // Platform HITL — do not rely on the LLM to call ask_user.
                        var asked = TryAskRowLimitInteractive(sessionKey, totalCount, out var chosen);
                        log.Info(
                            "[RowLimit] ask_user asked={0} chosen={1} count={2} session={3} onAskBound={4}",
                            asked, chosen ?? "(null)", totalCount, sessionKey ?? "(null)", onAskBound);
                        if (!asked)
                        {
                            log.Warn("[RowLimit] soft-fail need_ask_user count={0} session={1}", totalCount, sessionKey ?? "(null)");
                            return JsonConvert.SerializeObject(new
                            {
                                IsSuccess = false,
                                ok = false,
                                need_ask_user = true,
                                contextKey = AgentRowLimitConfirmBL.ContextKey,
                                counted_rows = totalCount,
                                prompt = AgentRowLimitConfirmBL.AskUserPrompt(totalCount),
                                optionsJson = AgentRowLimitConfirmBL.AskUserOptionsJson(totalCount, sessionKey),
                                Error = AgentRowLimitConfirmBL.SoftFailMessage(totalCount)
                            });
                        }
                        if (chosen == null)
                        {
                            log.Info("[RowLimit] user cancelled count={0} session={1}", totalCount, sessionKey ?? "(null)");
                            return JsonConvert.SerializeObject(new
                            {
                                IsSuccess = false,
                                ok = false,
                                cancelled = true,
                                counted_rows = totalCount,
                                Error = "User cancelled row-limit choice."
                            });
                        }
                        rowCap = AgentRowLimitConfirmBL.MaxRowsForOption(chosen);
                        displayRowLimit = rowCap;
                        forceTop = true;
                        sqlToRun = AgentRowLimitConfirmBL.StripTopOrLimit(sql);
                        hasChoice = true;
                        gatePath = "askUserChosen:" + chosen;
                    }
                }
                else
                {
                    if (AgentRowLimitConfirmBL.TryGetHardTopRowLimit(sql, out var hardTop))
                    {
                        rowCap = hardTop;
                        displayRowLimit = hardTop;
                        forceTop = false;
                        gatePath = "skipGateHardTop";
                    }
                    else
                    {
                        rowCap = AgentDataAnalyzeCacheBL.MaxCachedRows;
                        forceTop = false;
                        gatePath = "skipGate";
                    }
                }

                log.Info(
                    "[RowLimit] fetch path={0} rowCap={1} forceTop={2} counted={3} session={4}",
                    gatePath, rowCap, forceTop, countedRows?.ToString() ?? "(n/a)", sessionKey ?? "(null)");

                var result = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL                     = sqlToRun,
                    DataSourceRegisterId    = ds,
                    RequireConfirmation     = false,
                    IsConfirmed             = true,
                    MaxRows                 = rowCap,
                    ForceRewriteExistingTop = forceTop
                });

                if (!result.IsSuccess)
                    return JsonConvert.SerializeObject(new { result.IsSuccess, result.Error });

                // Tools path: return full rows (≤ rowCap / MaxCachedRows). GenericAgent's AgentDataAnalyzeHook
                // caches them for render/analyze, then rewrites the LLM-facing payload to
                // LlmPreviewMaxRows (20) sample + envelope. Do not Take(20) here.
                var all = result.Results
                    ?? new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
                var total = all.Count;
                var hardCap = AgentDataAnalyzeCacheBL.MaxCachedRows;
                var returned = total > hardCap ? all.Take(hardCap).ToList() : all;

                log.Info(
                    "[RowLimit] done path={0} returnedRows={1} rowCap={2} session={3}",
                    gatePath, returned.Count, rowCap, sessionKey ?? "(null)");

                displayRowLimit = AgentDisplayRowLimitBL.Coalesce(
                    displayRowLimit,
                    AgentDisplayRowLimitBL.TryGetTurnRowIntent(sessionKey, out var turnCap) ? turnCap : null);

                return JsonConvert.SerializeObject(new
                {
                    result.IsSuccess,
                    DataSourceId = ds,
                    result.ColumnNames,
                    RowCount = total,
                    ReturnedRows = returned.Count,
                    Truncated = total > returned.Count,
                    counted_rows = countedRows,
                    row_limit_confirmed = hasChoice,
                    row_limit_cap = rowCap,
                    row_limit_path = gatePath,
                    display_row_limit = displayRowLimit,
                    Rows = returned
                }, Formatting.Indented);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message }); }
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("insert_mockup_data",
            "Insert realistic sample/demo rows into a database table so the app has data to show. " +
            "Call this AFTER create_application and create_search_view succeed. " +
            "Only INSERT statements are allowed. " +
            "Generate enough rows to demonstrate every dropdown, relationship, and field (typically 5-15 rows per table). " +
            "Start with lookup/reference tables (no FK dependencies), then master rows, then child rows. " +
            "Do NOT wrap in a transaction — execute each INSERT individually so partial success is preserved. " +
            "Returns a count of rows inserted and any errors.")]
        public string InsertMockupData(
            [AgentParam("Table name being populated (for labelling only)", isRequired: true)] string tableName,
            [AgentParam("One or more SQL INSERT statements separated by semicolons. Must only contain INSERT INTO statements.", isRequired: true)] string insertSql)
        {
            try
            {
                if (!_dataSourceId.HasValue)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "No data source configured." });

                // Only allow INSERT statements
                var statements = insertSql
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

                var errors   = new System.Collections.Generic.List<string>();
                int inserted = 0;

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(_dataSourceId.Value);

                foreach (var stmt in statements)
                {
                    if (!stmt.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Skipped non-INSERT statement: {stmt.Substring(0, Math.Min(80, stmt.Length))}");
                        continue;
                    }
                    try
                    {
                        fixture.ExecuteNonQueryResult(stmt, new System.Collections.Generic.List<System.Data.Common.DbParameter>());
                        inserted++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error on statement #{inserted + errors.Count + 1}: {ex.Message}");
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess  = errors.Count == 0,
                    TableName  = tableName,
                    Inserted   = inserted,
                    Errors     = errors.Count > 0 ? errors : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("check_table_exists",
            "Check whether a specific database table exists. Returns true/false. Optional dataSourceId targets a registered DataSource; omit to use the session default.")]
        public string CheckTableExists(
            [AgentParam("Table name to check", isRequired: true)] string tableName,
            [AgentParam("Schema owner, default 'dbo'")] string schemaOwner = "dbo",
            [AgentParam("Optional DataSourceRegisterId. When omitted or 0, uses the session default data source.")] int? dataSourceId = null)
        {
            try
            {
                var ds = ResolveDataSourceId(dataSourceId);
                var result = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL = $"SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES " +
                          $"WHERE TABLE_NAME = '{tableName.Replace("'", "''")}' " +
                          $"AND TABLE_SCHEMA = '{(schemaOwner ?? "dbo").Replace("'", "''")}'",
                    DataSourceRegisterId = ds,
                    RequireConfirmation  = false,
                    IsConfirmed          = true
                });

                bool exists = false;
                if (result.IsSuccess && result.Results?.Count > 0)
                    exists = result.Results[0].ContainsKey("TableCount")
                          && result.Results[0]["TableCount"]?.ToString() != "0";

                return JsonConvert.SerializeObject(new { TableName = tableName, Exists = exists, DataSourceId = ds });
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message }); }
        }

        private int? ResolveDataSourceId(int? dataSourceId)
            => dataSourceId is > 0 ? dataSourceId : _dataSourceId;

        /// <summary>
        /// Blocks on chat UI ask_user for row limit. Returns false if HITL not wired;
        /// chosen=null when user cancelled.
        /// </summary>
        private static bool TryAskRowLimitInteractive(string sessionKey, long totalCount, out string chosen)
        {
            chosen = null;
            var bridge = AgentHitlBridge.Resolve(sessionKey);
            var onAsk = bridge?.OnAskUser;
            if (onAsk == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(
                    "[RowLimit] OnAskUser not bound (chatSessionKey={0}, bridgeNull={1})",
                    sessionKey ?? "", bridge == null);
                return false;
            }

            var options = AgentRowLimitConfirmBL.BuildOptions(totalCount, sessionKey)
                .Select(o => new LookupItemDto { Id = o.Id, Display = o.Label })
                .ToList();
            var askEvent = new AgentAskUserEvent
            {
                Prompt = AgentRowLimitConfirmBL.AskUserPrompt(totalCount),
                Mode = "single_choice",
                Ui = "button_group",
                Layout = "vertical",
                Options = options,
                ContextKey = AgentRowLimitConfirmBL.ContextKey
            };

            AgentAskUserResponse response;
            try
            {
                // Tools run under Task.Run — must not block a sync context; ConfigureAwait(false) is required.
                response = onAsk(askEvent).ConfigureAwait(false).GetAwaiter().GetResult()
                    ?? new AgentAskUserResponse { Cancelled = true };
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex,
                    "Row-limit ask_user failed (chatSessionKey={0}, counted={1})", sessionKey ?? "", totalCount);
                return false;
            }

            if (response.Cancelled) return true; // asked, cancelled
            if (!AgentRowLimitConfirmBL.LooksLikeChoice(response.SelectedIds, response.FreeText, out var opt))
                return true; // asked, no parseable choice → treat as cancel
            if (!string.IsNullOrWhiteSpace(sessionKey))
                AgentRowLimitConfirmBL.Mark(sessionKey, opt);
            chosen = opt;
            return true;
        }
    }
}
