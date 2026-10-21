using System;
using System.Linq;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using App.BL.DbGenie;
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
            + "For unbounded detail SELECTs the platform COUNTs first: ≤250 rows → fetch all; larger → ask_user confirm_row_limit with count-based options. "
            + "Do not invent TOP yourself. Scalar COUNT(*) / GROUP BY may omit TOP. "
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

                int rowCap;
                bool forceTop;
                long? countedRows = null;

                if (hasChoice)
                {
                    rowCap = chosenMax;
                    forceTop = true;
                }
                else if (AgentRowLimitConfirmBL.NeedsRowLimitConfirm(sql))
                {
                    // COUNT first — only ask when the real total is large.
                    var countSql = AgentRowLimitConfirmBL.BuildCountSql(sql);
                    var countResult = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                    {
                        SQL                  = countSql,
                        DataSourceRegisterId = ds,
                        RequireConfirmation  = false,
                        IsConfirmed          = true,
                        MaxRows              = 1
                    });
                    if (!countResult.IsSuccess)
                        return JsonConvert.SerializeObject(new { countResult.IsSuccess, countResult.Error, step = "count" });

                    if (!AgentRowLimitConfirmBL.TryParseCount(countResult.Results, out var totalCount))
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            Error = "Could not parse COUNT result for row-limit gate."
                        });

                    countedRows = totalCount;
                    if (!string.IsNullOrWhiteSpace(sessionKey))
                        AgentRowLimitConfirmBL.RememberCount(sessionKey, totalCount);

                    if (AgentRowLimitConfirmBL.ShouldAutoShowAll(totalCount))
                    {
                        rowCap = AgentDataAnalyzeCacheBL.MaxCachedRows;
                        forceTop = true; // ensure missing TOP gets a safety cap
                    }
                    else if (deterministic)
                    {
                        // No UI ask — cap at 1000 for deterministic runs.
                        rowCap = 1000;
                        forceTop = true;
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            ok = false,
                            need_ask_user = true,
                            contextKey = AgentRowLimitConfirmBL.ContextKey,
                            counted_rows = totalCount,
                            prompt = AgentRowLimitConfirmBL.AskUserPrompt(totalCount),
                            optionsJson = AgentRowLimitConfirmBL.AskUserOptionsJson(totalCount),
                            Error = AgentRowLimitConfirmBL.SoftFailMessage(totalCount)
                        });
                    }
                }
                else
                {
                    // Honor model's explicit TOP; aggregates / GROUP BY skip the gate.
                    rowCap = AgentDataAnalyzeCacheBL.MaxCachedRows;
                    forceTop = false;
                }

                var result = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL                     = sql,
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
    }
}
