using System;
using System.Text;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Shared SQL / Input / Output formatting for Think Details and Tool Activity.
    /// </summary>
    internal static class ToolIoFormatter
    {
        private const int MaxInputChars  = 4000;
        private const int MaxOutputChars = 4000;
        private const int MaxSqlChars    = 8000;

        public static string FormatToolCallDetails(string toolName, KernelArguments args)
        {
            var sb = new StringBuilder();
            var sql = ExtractSql(args);
            var inputJson = ArgsAsJson(args, excludeSql: !string.IsNullOrEmpty(sql));

            if (!string.IsNullOrEmpty(sql))
            {
                sb.AppendLine("## SQL");
                sb.AppendLine("```sql");
                sb.AppendLine(Truncate(sql, MaxSqlChars));
                sb.AppendLine("```");
            }

            if (!string.IsNullOrWhiteSpace(inputJson) && inputJson != "{}")
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("## Input");
                sb.AppendLine("```json");
                sb.AppendLine(Truncate(inputJson, MaxInputChars));
                sb.AppendLine("```");
            }

            if (sb.Length == 0)
                return Truncate(SafeSerialize(args), MaxInputChars) ?? "";

            return sb.ToString().TrimEnd();
        }

        public static string FormatToolResultDetails(string toolName, KernelArguments args, string resultText, bool ok)
        {
            var sb = new StringBuilder();
            var sql = ExtractSql(args);
            if (!string.IsNullOrEmpty(sql) && IsSqlTool(toolName))
            {
                sb.AppendLine("## SQL");
                sb.AppendLine("```sql");
                sb.AppendLine(Truncate(sql, MaxSqlChars));
                sb.AppendLine("```");
                sb.AppendLine();
            }

            sb.AppendLine(ok ? "## Output" : "## Error");
            var body = PrettyOrRaw(resultText);
            if (LooksLikeJson(body))
            {
                sb.AppendLine("```json");
                sb.AppendLine(Truncate(body, MaxOutputChars));
                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine("```");
                sb.AppendLine(Truncate(body ?? "", MaxOutputChars));
                sb.AppendLine("```");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>Expandable Think detail: Input/SQL before tool; Output after.</summary>
        public static string FormatThinkDetail(string toolName, KernelArguments args, string resultText, bool ok, bool afterTool)
        {
            if (!afterTool)
                return FormatToolCallDetails(toolName, args);

            return FormatToolResultDetails(toolName, args, resultText, ok);
        }

        private static bool IsSqlTool(string toolName)
        {
            var n = (toolName ?? "").Trim();
            return string.Equals(n, "execute_sql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(n, "run_sql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(n, "sql_query", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractSql(KernelArguments args)
        {
            if (args == null) return null;
            foreach (var key in new[] { "sql", "query", "statement", "Sql", "Query" })
            {
                if (args.TryGetValue(key, out var v) && v != null)
                {
                    var s = (v as string ?? v.ToString())?.Trim();
                    if (!string.IsNullOrEmpty(s) && LooksLikeSql(s))
                        return s;
                }
            }
            // Case-insensitive scan
            foreach (var kv in args)
            {
                if (kv.Value == null) continue;
                if (!IsSqlArgName(kv.Key)) continue;
                var s = (kv.Value as string ?? kv.Value.ToString())?.Trim();
                if (!string.IsNullOrEmpty(s) && LooksLikeSql(s))
                    return s;
            }
            return null;
        }

        private static bool IsSqlArgName(string key) =>
            string.Equals(key, "sql", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "query", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "statement", StringComparison.OrdinalIgnoreCase);

        private static bool LooksLikeSql(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length < 6) return false;
            var t = s.TrimStart();
            return t.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("DECLARE", StringComparison.OrdinalIgnoreCase);
        }

        private static string ArgsAsJson(KernelArguments args, bool excludeSql)
        {
            if (args == null) return "{}";
            var jo = new JObject();
            foreach (var kv in args)
            {
                if (kv.Value == null) continue;
                if (excludeSql && IsSqlArgName(kv.Key)) continue;
                try
                {
                    if (kv.Value is string s)
                    {
                        // Nested JSON string → parse for pretty later
                        if ((s.StartsWith("{") || s.StartsWith("[")) && s.Length < MaxInputChars)
                        {
                            try { jo[kv.Key] = JToken.Parse(s); continue; }
                            catch { /* keep as string */ }
                        }
                        jo[kv.Key] = s;
                    }
                    else
                        jo[kv.Key] = JToken.FromObject(kv.Value);
                }
                catch
                {
                    jo[kv.Key] = kv.Value.ToString();
                }
            }
            return jo.ToString(Formatting.Indented);
        }

        private static string PrettyOrRaw(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text ?? "";
            try
            {
                var token = JToken.Parse(text);
                return token.ToString(Formatting.Indented);
            }
            catch
            {
                return text;
            }
        }

        private static bool LooksLikeJson(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.TrimStart();
            return t.StartsWith("{") || t.StartsWith("[");
        }

        private static string SafeSerialize(object obj)
        {
            try { return JsonConvert.SerializeObject(obj, Formatting.Indented); }
            catch { return ""; }
        }

        private static string Truncate(string s, int max)
        {
            if (s == null || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }
    }
}
