using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.TenantBusiness.AgentToolExecutors
{
    /// <summary>
    /// Executes a parameterized SQL SELECT query against the tenant database.
    /// ToolConfig: {"Sql":"SELECT * FROM MyTable WHERE Id=@id","Params":["@id"]}
    /// Args dict keys match param names (without @).
    /// </summary>
    public static class SqlQueryToolExecutor
    {
        public static async Task<string> ExecuteAsync(
            string                             toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct)
        {
            var cfg = ParseConfig(toolConfig);
            if (string.IsNullOrWhiteSpace(cfg.Sql))
                return JsonConvert.SerializeObject(new { Error = "SqlQuery ToolConfig requires Sql." });
            if (string.IsNullOrWhiteSpace(context.ConnectionString))
                return JsonConvert.SerializeObject(new { Error = "No connection string in context." });

            try
            {
                using var conn = new SqlConnection(context.ConnectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);
                using var cmd = new SqlCommand(cfg.Sql, conn);
                cmd.CommandTimeout = 60;

                foreach (var paramName in cfg.Params)
                {
                    var key = paramName.TrimStart('@');
                    var value = args != null && args.TryGetValue(key, out var v) ? (object)v : DBNull.Value;
                    cmd.Parameters.AddWithValue(paramName, value);
                }

                using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                var rows = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(row);
                    if (rows.Count >= 200) break; // row cap
                }

                return JsonConvert.SerializeObject(new { Rows = rows, Count = rows.Count });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }

        private static (string Sql, List<string> Params) ParseConfig(string toolConfig)
        {
            try
            {
                var obj = JObject.Parse(toolConfig ?? "{}");
                var sql = obj["Sql"]?.ToString() ?? "";
                var paramList = new List<string>();
                var paramsArr = obj["Params"] as JArray;
                if (paramsArr != null)
                    foreach (var t in paramsArr)
                        if (t.Type == JTokenType.String)
                            paramList.Add(t.ToString());
                return (sql, paramList);
            }
            catch { return ("", new List<string>()); }
        }
    }
}
