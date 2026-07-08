using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using APP.Components.EntityDto;
using APP.Framework;
using APP.LBL.DatabaseSpecific;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL
{
    public static class AppReportTemplateService
    {
        // ── Data fetch ────────────────────────────────────────────────────────

        /// <summary>
        /// Calls all configured data sources (SP or API) and merges results into a single
        /// token context.  The primary source (first in list) maps to the backward-compatible
        /// keys "header", "rs1", "rs2" …  Additional sources use their alias as a prefix:
        ///   alias "bom"  →  "bom" (scalar), "bom_rs1" (list), "bom_rs2" (list) …
        /// </summary>
        public static Dictionary<string, object> FetchData(
            AppReportTemplateDto template,
            int mainReferenceId,
            int? masterReferenceId = null,
            Dictionary<string, string> extraParams = null)
        {
            var sources = ParseDataSources(template);
            if (sources.Count == 0)
                return new Dictionary<string, object>();

            var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var extra   = ParseExtraParams(template);   // re-use ExtraParamDef parsing

            for (int si = 0; si < sources.Count; si++)
            {
                var src     = sources[si];
                bool isPrimary = si == 0;

                if (src.Type == "api")
                {
                    // API sources: placeholder — implement HTTP fetch when needed
                    continue;
                }

                // Stored-procedure source
                try
                {
                    using var conn = new SqlConnection(GetConnectionString());
                    conn.Open();
                    using var cmd = new SqlCommand(src.Value, conn);
                    cmd.CommandType    = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60;

                    cmd.Parameters.AddWithValue("@MainReferenceId",   mainReferenceId);
                    cmd.Parameters.AddWithValue("@MasterReferenceId", (object)masterReferenceId ?? DBNull.Value);

                    // Extra params apply to every SP
                    if (extra != null && extraParams != null)
                    {
                        foreach (var def in extra)
                        {
                            var val = extraParams.ContainsKey(def.Name) ? extraParams[def.Name] : def.DefaultValue;
                            cmd.Parameters.AddWithValue("@" + def.Name, (object)val ?? DBNull.Value);
                        }
                    }

                    var ds = new DataSet();
                    using (var da = new SqlDataAdapter(cmd)) da.Fill(ds);

                    MergeResultSets(context, ds, src.Name);
                }
                catch { /* individual source errors don't kill the whole render */ }
            }

            return context;
        }

        // ── Token rendering ───────────────────────────────────────────────────

        public static string RenderTemplate(string templateHtml, AppReportTemplateDto template, Dictionary<string, object> context)
        {
            if (string.IsNullOrWhiteSpace(templateHtml)) return string.Empty;
            var html = InjectPageCss(templateHtml, template);
            html = RenderEachBlocks(html, context);
            html = RenderIfBlocks(html, context);
            html = RenderScalars(html, context);
            return html;
        }

        // ── Token discovery ───────────────────────────────────────────────────

        /// <summary>
        /// Calls every configured data source with sample data to discover available tokens.
        /// Tokens from additional (non-primary) sources are prefixed with their alias.
        /// </summary>
        public static List<TokenDescriptor> GetAvailableTokens(AppReportTemplateDto template)
        {
            var tokens  = new List<TokenDescriptor>();
            var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var sources = ParseDataSources(template);

            for (int si = 0; si < sources.Count; si++)
            {
                var src = sources[si];

                if (src.Type == "api")
                {
                    // API: discover tokens from sampleJson pasted by the designer
                    if (!string.IsNullOrWhiteSpace(src.SampleJson))
                        MergeApiSampleJson(context, src.SampleJson, src.Name);
                    continue;
                }

                if (src.Type != "sp" || string.IsNullOrEmpty(src.Value)) continue;
                try
                {
                    using var conn = new SqlConnection(GetConnectionString());
                    conn.Open();
                    using var cmd = new SqlCommand(src.Value, conn);
                    cmd.CommandType    = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@MainReferenceId",   0);
                    cmd.Parameters.AddWithValue("@MasterReferenceId", DBNull.Value);
                    var ds = new DataSet();
                    using (var da = new SqlDataAdapter(cmd)) da.Fill(ds);
                    MergeResultSets(context, ds, src.Name);
                }
                catch { }
            }

            foreach (var kvp in context)
            {
                if (kvp.Value is Dictionary<string, object> scalarRow)
                {
                    foreach (var field in scalarRow.Keys)
                        tokens.Add(new TokenDescriptor
                        {
                            Token     = $"{{{{{kvp.Key}.{field}}}}}",
                            ResultSet = kvp.Key, Field = field, IsList = false
                        });
                }
                else if (kvp.Value is List<Dictionary<string, object>> rows && rows.Count > 0)
                {
                    tokens.Add(new TokenDescriptor
                    {
                        Token     = $"{{{{#each {kvp.Key}}}}}...{{{{/each}}}}",
                        ResultSet = kvp.Key, Field = "*", IsList = true
                    });
                    foreach (var field in rows[0].Keys)
                        tokens.Add(new TokenDescriptor
                        {
                            Token     = $"{{{{{field}}}}}",
                            ResultSet = kvp.Key, Field = field, IsList = true, InsideEach = true
                        });
                }
            }

            return tokens;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private static string GetConnectionString()
        {
            using var adapter = AppTenantAdapterBL.GetTenantAdapter();
            return adapter.ConnectionString;
        }

        /// <summary>
        /// Parses the configured data sources.
        /// Priority:  ExtraParamConfig.dataSources (new multi-source JSON)
        ///       then DataSpName (legacy single-SP string, backward-compat).
        /// </summary>
        private static List<DataSourceConfig> ParseDataSources(AppReportTemplateDto template)
        {
            // 1. New format: ExtraParamConfig contains a TemplateConfig JSON object
            if (!string.IsNullOrWhiteSpace(template?.ExtraParamConfig))
            {
                try
                {
                    var trimmed = template.ExtraParamConfig.Trim();
                    if (trimmed.StartsWith("{"))
                    {
                        var cfg = JsonConvert.DeserializeObject<TemplateConfig>(trimmed);
                        if (cfg?.DataSources?.Count > 0)
                            return cfg.DataSources;
                    }
                }
                catch { }
            }

            // 2. Legacy: single SP in DataSpName
            if (!string.IsNullOrWhiteSpace(template?.DataSpName))
                return new List<DataSourceConfig>
                {
                    new DataSourceConfig { Name = "header", Type = "sp", Value = template.DataSpName }
                };

            return new List<DataSourceConfig>();
        }

        private static List<ExtraParamDef> ParseExtraParams(AppReportTemplateDto template)
        {
            if (string.IsNullOrWhiteSpace(template?.ExtraParamConfig)) return null;
            try
            {
                var trimmed = template.ExtraParamConfig.Trim();
                if (trimmed.StartsWith("{"))
                {
                    var cfg = JsonConvert.DeserializeObject<TemplateConfig>(trimmed);
                    return cfg?.ExtraParams;
                }
                // Legacy: plain array
                if (trimmed.StartsWith("["))
                    return JsonConvert.DeserializeObject<List<ExtraParamDef>>(trimmed);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Merges DataSet result sets into the shared context.
        /// Primary source: RS0 → "header", RS1 → "rs1", RS2 → "rs2" …
        /// Additional source (alias "bom"): RS0 → "bom", RS1 → "bom_rs1", RS2 → "bom_rs2" …
        /// </summary>
        private static void MergeResultSets(
            Dictionary<string, object> context,
            DataSet ds, string alias)
        {
            for (int i = 0; i < ds.Tables.Count; i++)
            {
                var dt = ds.Tables[i];
                // Every source uses its alias: RS0 → alias, RS1 → alias_rs1, RS2 → alias_rs2 …
                string key = i == 0 ? alias : $"{alias}_rs{i}";

                if (i == 0 && dt.Rows.Count == 1)
                {
                    var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataColumn col in dt.Columns)
                        row[col.ColumnName] = dt.Rows[0][col] == DBNull.Value ? null : dt.Rows[0][col];
                    context[key] = row;
                }
                else
                {
                    var rows = new List<Dictionary<string, object>>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        foreach (DataColumn col in dt.Columns)
                            row[col.ColumnName] = dr[col] == DBNull.Value ? null : dr[col];
                        rows.Add(row);
                    }
                    context[key] = rows;
                }
            }
        }

        /// <summary>
        /// Discovers tokens from a sample JSON string pasted by the designer for an API source.
        /// JObject  → RS0 scalar keys; each JArray property → RS1, RS2 … list keys.
        /// JArray   → RS0 as a list.
        /// </summary>
        private static void MergeApiSampleJson(Dictionary<string, object> context, string sampleJson, string name)
        {
            if (string.IsNullOrWhiteSpace(sampleJson)) return;
            try
            {
                var token = JToken.Parse(sampleJson);
                if (token is JObject obj)
                {
                    var scalarRow = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    int arrayIdx  = 1;
                    foreach (var prop in obj.Properties())
                    {
                        if (prop.Value is JArray arr)
                        {
                            var rows = BuildRowsFromJArray(arr);
                            if (rows.Count > 0) context[$"{name}_rs{arrayIdx++}"] = rows;
                        }
                        else
                        {
                            scalarRow[prop.Name] = prop.Value?.ToObject<object>();
                        }
                    }
                    if (scalarRow.Count > 0) context[name] = scalarRow;
                }
                else if (token is JArray arr)
                {
                    var rows = BuildRowsFromJArray(arr);
                    if (rows.Count > 0) context[name] = rows;
                }
            }
            catch { }
        }

        private static List<Dictionary<string, object>> BuildRowsFromJArray(JArray arr)
        {
            var rows = new List<Dictionary<string, object>>();
            foreach (var item in arr)
            {
                if (item is JObject itemObj)
                {
                    var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in itemObj.Properties())
                        row[p.Name] = p.Value?.ToObject<object>();
                    rows.Add(row);
                }
            }
            return rows;
        }

        private static string InjectPageCss(string html, AppReportTemplateDto t)
        {
            string size   = $"{t?.PageSize ?? "A4"} {t?.Orientation ?? "portrait"}";
            int    margin = t?.MarginMm ?? 15;
            string css    = $"<style>@page{{size:{size};margin:{margin}mm}}</style>";
            return html.Contains("<head>") ? html.Replace("<head>", $"<head>{css}") : css + html;
        }

        private static string RenderEachBlocks(string html, Dictionary<string, object> context)
        {
            return Regex.Replace(html, @"\{\{#each\s+(\w+)\}\}([\s\S]*?)\{\{/each\}\}", m =>
            {
                string key   = m.Groups[1].Value;
                string inner = m.Groups[2].Value;
                if (!context.TryGetValue(key, out var val) || !(val is List<Dictionary<string, object>> rows))
                    return string.Empty;
                var sb = new StringBuilder();
                foreach (var row in rows)
                {
                    string rowHtml = inner;
                    foreach (var field in row)
                        rowHtml = rowHtml.Replace($"{{{{{field.Key}}}}}", field.Value?.ToString() ?? string.Empty);
                    sb.Append(rowHtml);
                }
                return sb.ToString();
            });
        }

        private static string RenderIfBlocks(string html, Dictionary<string, object> context)
        {
            return Regex.Replace(html, @"\{\{#if\s+([\w.]+)\}\}([\s\S]*?)\{\{/if\}\}", m =>
            {
                string path  = m.Groups[1].Value;
                string inner = m.Groups[2].Value;
                var    val   = ResolveToken(path, context);
                bool   truthy = val != null && val.ToString() is not ("" or "0" or "false");
                return truthy ? inner : string.Empty;
            });
        }

        private static string RenderScalars(string html, Dictionary<string, object> context)
        {
            return Regex.Replace(html, @"\{\{([\w.]+)\}\}", m =>
            {
                var val = ResolveToken(m.Groups[1].Value, context);
                return val?.ToString() ?? string.Empty;
            });
        }

        private static object ResolveToken(string path, Dictionary<string, object> context)
        {
            var parts = path.Split('.');
            if (parts.Length == 2 && context.TryGetValue(parts[0], out var section))
            {
                if (section is Dictionary<string, object> dict && dict.TryGetValue(parts[1], out var v))
                    return v;
            }
            else if (parts.Length == 1 && context.TryGetValue(parts[0], out var v))
                return v;
            return null;
        }
    }

    // ── Public model classes ──────────────────────────────────────────────────

    public class TokenDescriptor
    {
        public string Token      { get; set; }
        public string ResultSet  { get; set; }
        public string Field      { get; set; }
        public bool   IsList     { get; set; }
        public bool   InsideEach { get; set; }
    }

    public class DataSourceConfig
    {
        [JsonProperty("name")]       public string Name       { get; set; } = "header";
        [JsonProperty("type")]       public string Type       { get; set; } = "sp";   // "sp" | "api"
        [JsonProperty("value")]      public string Value      { get; set; }
        [JsonProperty("sampleJson")] public string SampleJson { get; set; }
    }

    /// <summary>
    /// JSON shape stored in AppReportTemplate.ExtraParamConfig for multi-source templates.
    /// Backward compat: if ExtraParamConfig is a plain JSON array it is treated as ExtraParams only.
    /// </summary>
    public class TemplateConfig
    {
        [JsonProperty("dataSources")] public List<DataSourceConfig> DataSources { get; set; }
        [JsonProperty("extraParams")]  public List<ExtraParamDef>   ExtraParams  { get; set; }
    }

    public class ExtraParamDef
    {
        public string Name         { get; set; }
        public string Label        { get; set; }
        public string DefaultValue { get; set; }
    }
}
