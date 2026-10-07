using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using App.BL;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.StoredProcedure
{
    /// <summary>
    /// List / search / detail for stored procedures on a registered DataSource.
    /// Supports SQL Server, MySQL, Oracle via DatabaseFixture.SqlServerType.
    /// </summary>
    public static class StoredProcedureCatalogBL
    {
        private static readonly ConcurrentDictionary<int, (DateTime At, List<StoredProcedureListItem> Items)> ListCache
            = new ConcurrentDictionary<int, (DateTime, List<StoredProcedureListItem>)>();

        private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(5);

        public static IStoredProcedureAllowList AllowList { get; set; } = AllowAllStoredProcedureAllowList.Instance;

        public static string ListJson(int dataSourceId, string schema = null, int skip = 0, int take = 50)
        {
            try
            {
                var all = LoadList(dataSourceId);
                var filtered = string.IsNullOrWhiteSpace(schema)
                    ? all
                    : all.Where(x => string.Equals(x.Schema, schema.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

                skip = Math.Max(0, skip);
                take = Math.Clamp(take <= 0 ? 50 : take, 1, 200);
                var page = filtered.Skip(skip).Take(take).ToList();
                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    dataSourceId,
                    total = filtered.Count,
                    skip,
                    take,
                    procedures = page,
                    next = "Use stored_procedure_search to find by keyword, then stored_procedure_detail for parameters and full definition, then stored_procedure_execute."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return Err(ex.Message, dataSourceId);
            }
        }

        public static string SearchJson(int dataSourceId, string query, int take = 30)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Err("query is required (name, schema, comment, or definition keyword).", dataSourceId);

                take = Math.Clamp(take <= 0 ? 30 : take, 1, 100);
                var needle = query.Trim();
                var all = LoadList(dataSourceId);

                // Name/schema/description first (fast path from cache).
                var hits = all
                    .Where(x =>
                        Contains(x.Name, needle) ||
                        Contains(x.Schema, needle) ||
                        Contains(x.Description, needle) ||
                        Contains(x.FullName, needle))
                    .Take(take)
                    .ToList();

                // If few hits, also scan definitions (slower; capped).
                if (hits.Count < take)
                {
                    var known = new HashSet<string>(hits.Select(h => h.FullName), StringComparer.OrdinalIgnoreCase);
                    var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                    var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
                    foreach (var item in all)
                    {
                        if (hits.Count >= take) break;
                        if (known.Contains(item.FullName)) continue;
                        var def = TryLoadDefinition(fixture, engine, item.Schema, item.Name);
                        if (!string.IsNullOrEmpty(def) && Contains(def, needle))
                        {
                            hits.Add(item);
                            known.Add(item.FullName);
                        }
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    dataSourceId,
                    query = needle,
                    count = hits.Count,
                    procedures = hits,
                    next = "Call stored_procedure_detail with procedureName (and schema if needed) before execute."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return Err(ex.Message, dataSourceId);
            }
        }

        public static string DetailJson(int dataSourceId, string procedureName, string schema = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(procedureName))
                    return Err("procedureName is required.", dataSourceId);

                ParseName(procedureName, schema, out var sch, out var name, dataSourceId);
                if (!AllowList.IsAllowed(dataSourceId, sch, name))
                    return Err($"Stored procedure '{FormatFull(sch, name)}' is not allowed.", dataSourceId);

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
                if (string.IsNullOrWhiteSpace(sch))
                    sch = DefaultSchema(engine, fixture);

                var parameters = LoadParameters(fixture, engine, sch, name);
                var definition = TryLoadDefinition(fixture, engine, sch, name);
                var description = LoadDescription(fixture, engine, sch, name)
                    ?? DescriptionFromList(dataSourceId, sch, name);

                var detail = new StoredProcedureDetailDto
                {
                    DataSourceId = dataSourceId,
                    Schema = sch,
                    Name = name,
                    FullName = FormatFull(sch, name),
                    Description = description,
                    Engine = engine.ToString(),
                    Parameters = parameters,
                    Definition = definition,
                    UsageHint = BuildUsageHint(parameters)
                };

                if (string.IsNullOrWhiteSpace(definition) && parameters.Count == 0)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"Stored procedure not found or inaccessible: {detail.FullName}",
                        dataSourceId
                    }, Formatting.Indented);
                }

                return JsonConvert.SerializeObject(new { ok = true, procedure = detail }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return Err(ex.Message, dataSourceId);
            }
        }

        internal static void ParseName(string procedureName, string schema, out string schemaOut, out string nameOut, int dataSourceId)
        {
            nameOut = (procedureName ?? "").Trim().Trim('[', ']', '`', '"');
            schemaOut = string.IsNullOrWhiteSpace(schema) ? null : schema.Trim().Trim('[', ']', '`', '"');

            if (schemaOut == null && nameOut.Contains("."))
            {
                var parts = nameOut.Split(new[] { '.' }, 2);
                schemaOut = parts[0].Trim().Trim('[', ']', '`', '"');
                nameOut = parts[1].Trim().Trim('[', ']', '`', '"');
            }

            if (string.IsNullOrWhiteSpace(schemaOut))
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                schemaOut = DefaultSchema(fixture.SqlServerType ?? EmSqlType.SqlServer, fixture);
            }
        }

        internal static string FormatFull(string schema, string name)
            => string.IsNullOrWhiteSpace(schema) ? name : schema + "." + name;

        private static string DefaultSchema(EmSqlType engine, DatabaseFixture fixture)
        {
            if (engine == EmSqlType.MySql)
            {
                try
                {
                    var dt = fixture.RetriveDataTable("SELECT DATABASE() AS db", new List<DbParameter>());
                    if (dt.Rows.Count > 0)
                        return dt.Rows[0]["db"]?.ToString() ?? "";
                }
                catch { /* fall through */ }
                return "";
            }
            if (engine == EmSqlType.Oracle)
            {
                try
                {
                    var dt = fixture.RetriveDataTable("SELECT USER AS u FROM DUAL", new List<DbParameter>());
                    if (dt.Rows.Count > 0)
                        return dt.Rows[0]["u"]?.ToString() ?? "";
                }
                catch { /* fall through */ }
                return "";
            }
            return "dbo";
        }

        private static List<StoredProcedureListItem> LoadList(int dataSourceId)
        {
            if (ListCache.TryGetValue(dataSourceId, out var cached)
                && DateTime.UtcNow - cached.At < ListCacheTtl)
                return cached.Items;

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
            var items = engine switch
            {
                EmSqlType.MySql => ListMySql(fixture),
                EmSqlType.Oracle => ListOracle(fixture),
                _ => ListSqlServer(fixture)
            };

            ListCache[dataSourceId] = (DateTime.UtcNow, items);
            return items;
        }

        private static List<StoredProcedureListItem> ListSqlServer(DatabaseFixture fixture)
        {
            const string sql = @"
SELECT SCHEMA_NAME(p.schema_id) AS SchemaName,
       p.name AS Name,
       CONVERT(nvarchar(4000), ep.value) AS Description,
       p.modify_date AS ModifiedAt
FROM sys.procedures p
LEFT JOIN sys.extended_properties ep
  ON ep.major_id = p.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description'
WHERE p.is_ms_shipped = 0
ORDER BY SchemaName, Name";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter>());
            return MapListRows(dt);
        }

        private static List<StoredProcedureListItem> ListMySql(DatabaseFixture fixture)
        {
            const string sql = @"
SELECT ROUTINE_SCHEMA AS SchemaName,
       ROUTINE_NAME AS Name,
       ROUTINE_COMMENT AS Description,
       LAST_ALTERED AS ModifiedAt
FROM information_schema.ROUTINES
WHERE ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_SCHEMA = DATABASE()
ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter>());
            return MapListRows(dt);
        }

        private static List<StoredProcedureListItem> ListOracle(DatabaseFixture fixture)
        {
            const string sql = @"
SELECT OWNER AS SchemaName,
       OBJECT_NAME AS Name,
       NULL AS Description,
       LAST_DDL_TIME AS ModifiedAt
FROM ALL_PROCEDURES
WHERE OBJECT_TYPE = 'PROCEDURE'
  AND OWNER = USER
ORDER BY OWNER, OBJECT_NAME";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter>());
            return MapListRows(dt);
        }

        private static List<StoredProcedureListItem> MapListRows(DataTable dt)
        {
            var list = new List<StoredProcedureListItem>();
            foreach (DataRow row in dt.Rows)
            {
                var schema = row["SchemaName"]?.ToString() ?? "";
                var name = row["Name"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(name)) continue;
                DateTime? modified = null;
                if (row.Table.Columns.Contains("ModifiedAt") && row["ModifiedAt"] != DBNull.Value
                    && DateTime.TryParse(row["ModifiedAt"].ToString(), out var m))
                    modified = m;
                list.Add(new StoredProcedureListItem
                {
                    Schema = schema,
                    Name = name,
                    FullName = FormatFull(schema, name),
                    Description = row.Table.Columns.Contains("Description") ? row["Description"]?.ToString() : null,
                    ModifiedAt = modified
                });
            }
            return list;
        }

        internal static List<StoredProcedureParameterDto> LoadParameters(
            DatabaseFixture fixture, EmSqlType engine, string schema, string name)
        {
            return engine switch
            {
                EmSqlType.MySql => ParamsMySql(fixture, schema, name),
                EmSqlType.Oracle => ParamsOracle(fixture, schema, name),
                _ => ParamsSqlServer(fixture, schema, name)
            };
        }

        private static List<StoredProcedureParameterDto> ParamsSqlServer(DatabaseFixture fixture, string schema, string name)
        {
            var pSchema = fixture.CreateParameter("@schema");
            pSchema.Value = schema ?? (object)DBNull.Value;
            var pName = fixture.CreateParameter("@name");
            pName.Value = name;
            const string sql = @"
SELECT p.name AS ParamName,
       TYPE_NAME(p.user_type_id) AS TypeName,
       p.max_length AS MaxLength,
       p.is_output AS IsOutput,
       p.has_default_value AS HasDefault,
       p.parameter_id AS Ordinal
FROM sys.parameters p
INNER JOIN sys.procedures pr ON pr.object_id = p.object_id
WHERE pr.name = @name
  AND SCHEMA_NAME(pr.schema_id) = @schema
ORDER BY p.parameter_id";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pSchema, pName });
            var list = new List<StoredProcedureParameterDto>();
            foreach (DataRow row in dt.Rows)
            {
                var isOut = row["IsOutput"] != DBNull.Value && Convert.ToBoolean(row["IsOutput"]);
                list.Add(new StoredProcedureParameterDto
                {
                    Name = row["ParamName"]?.ToString(),
                    Type = row["TypeName"]?.ToString(),
                    Direction = isOut ? "INOUT_OR_OUT" : "IN",
                    MaxLength = row["MaxLength"] == DBNull.Value ? null : Convert.ToInt32(row["MaxLength"]),
                    Ordinal = row["Ordinal"] == DBNull.Value ? 0 : Convert.ToInt32(row["Ordinal"]),
                    HasDefault = row["HasDefault"] != DBNull.Value && Convert.ToBoolean(row["HasDefault"])
                });
            }
            return list;
        }

        private static List<StoredProcedureParameterDto> ParamsMySql(DatabaseFixture fixture, string schema, string name)
        {
            var pSchema = fixture.CreateParameter("@schema");
            pSchema.Value = schema;
            var pName = fixture.CreateParameter("@name");
            pName.Value = name;
            const string sql = @"
SELECT PARAMETER_NAME AS ParamName,
       DATA_TYPE AS TypeName,
       PARAMETER_MODE AS Mode,
       CHARACTER_MAXIMUM_LENGTH AS MaxLength,
       ORDINAL_POSITION AS Ordinal
FROM information_schema.PARAMETERS
WHERE SPECIFIC_SCHEMA = @schema
  AND SPECIFIC_NAME = @name
  AND ROUTINE_TYPE = 'PROCEDURE'
ORDER BY ORDINAL_POSITION";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pSchema, pName });
            var list = new List<StoredProcedureParameterDto>();
            foreach (DataRow row in dt.Rows)
            {
                var mode = row["Mode"]?.ToString() ?? "IN";
                list.Add(new StoredProcedureParameterDto
                {
                    Name = row["ParamName"]?.ToString(),
                    Type = row["TypeName"]?.ToString(),
                    Direction = mode,
                    MaxLength = row["MaxLength"] == DBNull.Value || row["MaxLength"] == null
                        ? null : Convert.ToInt32(row["MaxLength"]),
                    Ordinal = row["Ordinal"] == DBNull.Value ? 0 : Convert.ToInt32(row["Ordinal"]),
                    HasDefault = false
                });
            }
            return list;
        }

        private static List<StoredProcedureParameterDto> ParamsOracle(DatabaseFixture fixture, string schema, string name)
        {
            var pSchema = fixture.CreateParameter("schema");
            pSchema.Value = schema;
            var pName = fixture.CreateParameter("name");
            pName.Value = name;
            const string sql = @"
SELECT ARGUMENT_NAME AS ParamName,
       DATA_TYPE AS TypeName,
       IN_OUT AS Mode,
       DATA_LENGTH AS MaxLength,
       POSITION AS Ordinal
FROM ALL_ARGUMENTS
WHERE OWNER = :schema
  AND OBJECT_NAME = :name
  AND PACKAGE_NAME IS NULL
  AND ARGUMENT_NAME IS NOT NULL
ORDER BY POSITION";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pSchema, pName });
            var list = new List<StoredProcedureParameterDto>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new StoredProcedureParameterDto
                {
                    Name = row["ParamName"]?.ToString(),
                    Type = row["TypeName"]?.ToString(),
                    Direction = row["Mode"]?.ToString() ?? "IN",
                    MaxLength = row["MaxLength"] == DBNull.Value || row["MaxLength"] == null
                        ? null : Convert.ToInt32(row["MaxLength"]),
                    Ordinal = row["Ordinal"] == DBNull.Value ? 0 : Convert.ToInt32(row["Ordinal"]),
                    HasDefault = false
                });
            }
            return list;
        }

        internal static string TryLoadDefinition(DatabaseFixture fixture, EmSqlType engine, string schema, string name)
        {
            try
            {
                return engine switch
                {
                    EmSqlType.MySql => DefMySql(fixture, schema, name),
                    EmSqlType.Oracle => DefOracle(fixture, schema, name),
                    _ => DefSqlServer(fixture, schema, name)
                };
            }
            catch
            {
                return null;
            }
        }

        private static string DefSqlServer(DatabaseFixture fixture, string schema, string name)
        {
            var pSchema = fixture.CreateParameter("@schema");
            pSchema.Value = schema;
            var pName = fixture.CreateParameter("@name");
            pName.Value = name;
            const string sql = @"
SELECT OBJECT_DEFINITION(p.object_id) AS Def
FROM sys.procedures p
WHERE p.name = @name AND SCHEMA_NAME(p.schema_id) = @schema";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pSchema, pName });
            if (dt.Rows.Count == 0) return null;
            return dt.Rows[0]["Def"]?.ToString();
        }

        private static string DefMySql(DatabaseFixture fixture, string schema, string name)
        {
            // ROUTINE_DEFINITION can be truncated; SHOW CREATE is fuller when permitted.
            try
            {
                var safeSchema = QuoteIdentMySql(schema);
                var safeName = QuoteIdentMySql(name);
                var dt = fixture.RetriveDataTable(
                    $"SHOW CREATE PROCEDURE {safeSchema}.{safeName}",
                    new List<DbParameter>());
                if (dt.Rows.Count > 0)
                {
                    foreach (DataColumn col in dt.Columns)
                    {
                        if (col.ColumnName.IndexOf("Create", StringComparison.OrdinalIgnoreCase) >= 0)
                            return dt.Rows[0][col]?.ToString();
                    }
                }
            }
            catch { /* fall back */ }

            var pSchema = fixture.CreateParameter("@schema");
            pSchema.Value = schema;
            var pName = fixture.CreateParameter("@name");
            pName.Value = name;
            const string sql = @"
SELECT ROUTINE_DEFINITION AS Def
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = @schema AND ROUTINE_NAME = @name AND ROUTINE_TYPE = 'PROCEDURE'";
            var dt2 = fixture.RetriveDataTable(sql, new List<DbParameter> { pSchema, pName });
            return dt2.Rows.Count == 0 ? null : dt2.Rows[0]["Def"]?.ToString();
        }

        private static string DefOracle(DatabaseFixture fixture, string schema, string name)
        {
            var pSchema = fixture.CreateParameter("schema");
            pSchema.Value = schema;
            var pName = fixture.CreateParameter("name");
            pName.Value = name;
            const string sql = @"
SELECT TEXT FROM ALL_SOURCE
WHERE OWNER = :schema AND NAME = :name AND TYPE = 'PROCEDURE'
ORDER BY LINE";
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pSchema, pName });
            if (dt.Rows.Count == 0) return null;
            var sb = new StringBuilder();
            foreach (DataRow row in dt.Rows)
                sb.Append(row["TEXT"]?.ToString());
            return sb.ToString();
        }

        private static string LoadDescription(DatabaseFixture fixture, EmSqlType engine, string schema, string name)
        {
            if (engine != EmSqlType.SqlServer) return null;
            try
            {
                var pSchema = fixture.CreateParameter("@schema");
                pSchema.Value = schema;
                var pName = fixture.CreateParameter("@name");
                pName.Value = name;
                const string sql = @"
SELECT CONVERT(nvarchar(4000), ep.value) AS Description
FROM sys.procedures p
LEFT JOIN sys.extended_properties ep
  ON ep.major_id = p.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description'
WHERE p.name = @name AND SCHEMA_NAME(p.schema_id) = @schema";
                var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pSchema, pName });
                return dt.Rows.Count == 0 ? null : dt.Rows[0]["Description"]?.ToString();
            }
            catch { return null; }
        }

        private static string DescriptionFromList(int dataSourceId, string schema, string name)
        {
            try
            {
                return LoadList(dataSourceId)
                    .FirstOrDefault(x =>
                        string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.Schema ?? "", schema ?? "", StringComparison.OrdinalIgnoreCase))
                    ?.Description;
            }
            catch { return null; }
        }

        private static string BuildUsageHint(List<StoredProcedureParameterDto> parameters)
        {
            var inputs = parameters
                .Where(p => !string.IsNullOrWhiteSpace(p.Name)
                    && !string.Equals(p.Direction, "OUT", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (inputs.Count == 0)
                return "Call stored_procedure_execute with procedureName; argsJson can be {} if no parameters.";

            var sample = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in inputs)
            {
                var key = p.Name.TrimStart('@');
                sample[key] = GuessSample(p.Type);
            }
            return "Pass argsJson as JSON object matching parameter names (with or without @), e.g. "
                   + JsonConvert.SerializeObject(sample);
        }

        private static object GuessSample(string type)
        {
            var t = (type ?? "").ToLowerInvariant();
            if (t.Contains("int") || t.Contains("decimal") || t.Contains("numeric") || t.Contains("float") || t.Contains("money"))
                return 0;
            if (t.Contains("bit") || t.Contains("bool"))
                return false;
            if (t.Contains("date") || t.Contains("time"))
                return "2020-01-01";
            return "value";
        }

        private static string QuoteIdentMySql(string ident)
            => "`" + (ident ?? "").Replace("`", "``") + "`";

        private static bool Contains(string hay, string needle)
            => !string.IsNullOrEmpty(hay)
               && hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string Err(string message, int dataSourceId)
            => JsonConvert.SerializeObject(new { ok = false, error = message, dataSourceId }, Formatting.Indented);

        /// <summary>Invalidate cached list after DDL (optional callers).</summary>
        public static void InvalidateCache(int dataSourceId)
            => ListCache.TryRemove(dataSourceId, out _);
    }
}
