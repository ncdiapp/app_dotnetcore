using System;
using System.Text;
using System.Text.Json;
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

        public static string ArgsAsOneLine(KernelArguments args)
            => AgentLoopLog.OneLine(ArgsAsJson(args, excludeSql: false), 2500);

        private static string ExtractSql(KernelArguments args)
        {
            if (args == null) return null;
            foreach (var key in new[] { "sql", "query", "statement", "Sql", "Query" })
            {
                if (args.TryGetValue(key, out var v) && v != null)
                {
                    var s = ArgToString(v)?.Trim();
                    if (!string.IsNullOrEmpty(s) && LooksLikeSql(s))
                        return s;
                }
            }
            foreach (var kv in args)
            {
                if (kv.Value == null) continue;
                if (!IsSqlArgName(kv.Key)) continue;
                var s = ArgToString(kv.Value)?.Trim();
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
                    var token = ToJToken(kv.Value);
                    if (token != null && token.Type == JTokenType.String)
                    {
                        var s = token.Value<string>() ?? "";
                        if ((s.StartsWith("{") || s.StartsWith("[")) && s.Length < MaxInputChars)
                        {
                            try { jo[kv.Key] = JToken.Parse(s); continue; }
                            catch { /* keep as string */ }
                        }
                    }
                    jo[kv.Key] = token;
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

        public static string ArgToString(object value)
        {
            if (value == null) return null;
            if (value is string s) return s;
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String ? je.GetString() : je.GetRawText();
            }
            return value.ToString();
        }

        /// <summary>SK/Gemini often pass System.Text.Json.JsonElement — unwrap so UI and logs show real strings.</summary>
        public static JToken ToJToken(object value)
        {
            if (value == null) return JValue.CreateNull();
            if (value is JToken jt) return jt;
            if (value is JsonElement je) return JsonElementToJToken(je);
            if (value is string s) return s;
            try { return JToken.FromObject(value); }
            catch { return new JValue(value.ToString()); }
        }

        private static JToken JsonElementToJToken(JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.String: return je.GetString() ?? "";
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined: return JValue.CreateNull();
                case JsonValueKind.Number:
                    if (je.TryGetInt64(out var l)) return l;
                    if (je.TryGetDouble(out var d)) return d;
                    return je.GetRawText();
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return JToken.Parse(je.GetRawText());
                default:
                    return je.GetRawText();
            }
        }
    }
}
