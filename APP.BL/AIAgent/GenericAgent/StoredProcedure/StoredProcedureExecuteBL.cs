using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using App.BL;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.StoredProcedure
{
    /// <summary>
    /// Execute a stored procedure on a registered DataSource. Allow-all in P0.
    /// </summary>
    public static class StoredProcedureExecuteBL
    {
        public const int MaxReturnRows = 100;

        public static string ExecuteJson(
            int dataSourceId,
            string procedureName,
            string schema = null,
            string argsJson = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(procedureName))
                    return Err("procedureName is required.", dataSourceId);

                StoredProcedureCatalogBL.ParseName(procedureName, schema, out var sch, out var name, dataSourceId);
                if (!StoredProcedureCatalogBL.AllowList.IsAllowed(dataSourceId, sch, name))
                    return Err($"Stored procedure '{StoredProcedureCatalogBL.FormatFull(sch, name)}' is not allowed.", dataSourceId);

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
                var fullName = BuildCommandName(engine, sch, name);
                var paramMeta = StoredProcedureCatalogBL.LoadParameters(fixture, engine, sch, name);

                var argMap = ParseArgs(argsJson);
                var dbParams = new List<DbParameter>();
                foreach (var p in paramMeta)
                {
                    var pName = p.Name;
                    if (string.IsNullOrWhiteSpace(pName)) continue;
                    var direction = p.Direction ?? "IN";
                    var bare = pName.TrimStart('@', ':');

                    var dbp = fixture.CreateParameter(bare);
                    if (IsOutput(direction))
                    {
                        dbp.Direction = ParameterDirection.InputOutput;
                        if (argMap.TryGetValue(bare, out var outVal) || argMap.TryGetValue(pName, out outVal))
                            dbp.Value = Coerce(outVal) ?? DBNull.Value;
                        else
                            dbp.Value = DBNull.Value;
                        if (dbp.DbType == DbType.String || dbp.DbType == DbType.AnsiString)
                            dbp.Size = Math.Max(dbp.Size, 4000);
                    }
                    else
                    {
                        dbp.Direction = ParameterDirection.Input;
                        if (argMap.TryGetValue(bare, out var inVal) || argMap.TryGetValue(pName, out inVal))
                            dbp.Value = Coerce(inVal) ?? DBNull.Value;
                        else
                            dbp.Value = DBNull.Value;
                    }
                    dbParams.Add(dbp);
                }

                // Extra args not in metadata — still bind (some engines omit metadata).
                foreach (var kv in argMap)
                {
                    var bare = kv.Key.TrimStart('@', ':');
                    if (dbParams.Any(x => string.Equals(
                            x.ParameterName.TrimStart('@', ':'), bare, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    var dbp = fixture.CreateParameter(bare);
                    dbp.Direction = ParameterDirection.Input;
                    dbp.Value = Coerce(kv.Value) ?? DBNull.Value;
                    dbParams.Add(dbp);
                }

                var table = fixture.RetriveStorProcDataTable(fullName, dbParams);
                var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                var rows = new List<Dictionary<string, object>>();
                var truncated = table.Rows.Count > MaxReturnRows;
                var take = Math.Min(table.Rows.Count, MaxReturnRows);
                for (int i = 0; i < take; i++)
                {
                    var row = table.Rows[i];
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataColumn col in table.Columns)
                    {
                        var v = row[col];
                        dict[col.ColumnName] = v == DBNull.Value ? null : v;
                    }
                    rows.Add(dict);
                }

                var outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var dbp in dbParams)
                {
                    if (dbp.Direction == ParameterDirection.Input) continue;
                    outputs[dbp.ParameterName] = dbp.Value == DBNull.Value ? null : dbp.Value;
                }

                // Full rows including nulls. Compact JSON only (no indent) to save CapResult budget.
                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    IsSuccess = true,
                    dataSourceId,
                    procedure = StoredProcedureCatalogBL.FormatFull(sch, name),
                    columnNames = columns,
                    rowCount = table.Rows.Count,
                    truncated,
                    rows,
                    outputParameters = outputs.Count > 0 ? outputs : null
                }, Formatting.None);
            }
            catch (Exception ex)
            {
                return Err(ex.Message, dataSourceId);
            }
        }

        private static string BuildCommandName(EmSqlType engine, string schema, string name)
        {
            if (string.IsNullOrWhiteSpace(schema)) return name;
            if (engine == EmSqlType.SqlServer)
                return "[" + schema.Replace("]", "]]") + "].[" + name.Replace("]", "]]") + "]";
            if (engine == EmSqlType.MySql)
                return "`" + schema.Replace("`", "``") + "`.`" + name.Replace("`", "``") + "`";
            // Oracle: SCHEMA.NAME
            return schema + "." + name;
        }

        private static Dictionary<string, JToken> ParseArgs(string argsJson)
        {
            var map = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(argsJson)) return map;
            try
            {
                var token = JToken.Parse(argsJson);
                if (token is JObject obj)
                {
                    foreach (var prop in obj.Properties())
                        map[prop.Name.TrimStart('@', ':')] = prop.Value;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("argsJson must be a JSON object of parameter name → value. " + ex.Message);
            }
            return map;
        }

        private static object Coerce(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            if (token.Type == JTokenType.Float) return token.Value<decimal>();
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            if (token.Type == JTokenType.Date) return token.Value<DateTime>();
            return token.ToString();
        }

        private static bool IsOutput(string direction)
        {
            var d = (direction ?? "").ToUpperInvariant();
            return d.Contains("OUT") || d.Contains("INOUT");
        }

        private static string Err(string message, int dataSourceId)
            => JsonConvert.SerializeObject(new
            {
                ok = false,
                IsSuccess = false,
                error = message,
                dataSourceId
            }, Formatting.Indented);
    }
}
