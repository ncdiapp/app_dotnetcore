using System;
using System.Linq;
using App.BL.DbGenie;
using APP.Components.EntityDto;
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

        [AgentFunction("execute_sql",
            "Execute a SQL SELECT query to verify tables were created or check data. Only SELECT is allowed.")]
        public string ExecuteSql(
            [AgentParam("A SQL SELECT statement. Must start with SELECT.", isRequired: true)] string sql)
        {
            try
            {
                if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = "Only SELECT statements are allowed. Use create_database_table for DDL."
                    });

                var result = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL                  = sql,
                    DataSourceRegisterId = _dataSourceId,
                    RequireConfirmation  = false,
                    IsConfirmed          = true
                });

                if (!result.IsSuccess)
                    return JsonConvert.SerializeObject(new { result.IsSuccess, result.Error });

                return JsonConvert.SerializeObject(new
                {
                    result.IsSuccess,
                    result.ColumnNames,
                    RowCount = result.Results?.Count ?? 0,
                    Rows     = result.Results?.Take(20).ToList()
                }, Formatting.Indented);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message }); }
        }

        [AgentFunction("insert_mockup_data",
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

        [AgentFunction("check_table_exists",
            "Check whether a specific database table exists. Returns true/false.")]
        public string CheckTableExists(
            [AgentParam("Table name to check", isRequired: true)] string tableName,
            [AgentParam("Schema owner, default 'dbo'")] string schemaOwner = "dbo")
        {
            try
            {
                var result = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL = $"SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES " +
                          $"WHERE TABLE_NAME = '{tableName.Replace("'", "''")}' " +
                          $"AND TABLE_SCHEMA = '{(schemaOwner ?? "dbo").Replace("'", "''")}'",
                    DataSourceRegisterId = _dataSourceId,
                    RequireConfirmation  = false,
                    IsConfirmed          = true
                });

                bool exists = false;
                if (result.IsSuccess && result.Results?.Count > 0)
                    exists = result.Results[0].ContainsKey("TableCount")
                          && result.Results[0]["TableCount"]?.ToString() != "0";

                return JsonConvert.SerializeObject(new { TableName = tableName, Exists = exists });
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message }); }
        }
    }
}
