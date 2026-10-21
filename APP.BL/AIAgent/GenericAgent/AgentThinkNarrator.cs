using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Builds concise Think Mode lines: why this tool, what was learned, what that implies next.
    /// Tool dumps stay in Tool Activity; chat only gets causal narrative.
    /// </summary>
    internal static class AgentThinkNarrator
    {
        public static string IntentBeforeTool(string toolName, KernelArguments args, string toolLabel)
        {
            var name = (toolName ?? "").Trim();
            var keyArgs = KeyArgs(args);

            if (IsAskUser(name))
                return NarrateAskUserIntent(keyArgs);

            if (Is(name, "api_provider_search"))
            {
                var q = First(keyArgs, "query", "keyword", "search", "q", "name");
                return string.IsNullOrEmpty(q)
                    ? "Need a registered API for this request — searching the API catalog."
                    : "Need a registered API for this request — searching catalog for \"" + Clip(q, 80) + "\".";
            }

            if (Is(name, "api_provider_detail", "api_provider_get"))
            {
                var id = First(keyArgs, "id", "apiId", "providerId", "name", "apiName");
                return string.IsNullOrEmpty(id)
                    ? "Need provider parameters before execute — loading API details."
                    : "Need execute parameters — loading details for API \"" + Clip(id, 60) + "\".";
            }

            if (Is(name, "api_provider_execute"))
            {
                var id = First(keyArgs, "id", "apiId", "providerId", "name", "apiName");
                return string.IsNullOrEmpty(id)
                    ? "Running the chosen API to get the data rows for analysis."
                    : "Running API \"" + Clip(id, 60) + "\" to get the data rows for analysis.";
            }

            if (Is(name, "get_database_schema", "database_schema", "schema_meta"))
            {
                var table = First(keyArgs, "table", "tableName", "name", "objectName");
                return string.IsNullOrEmpty(table)
                    ? "Need column definitions to group/filter correctly — reading database schema."
                    : "Need columns for grouping — reading schema of \"" + Clip(table, 60) + "\".";
            }

            if (Is(name, "execute_sql", "run_sql", "sql_query"))
            {
                var sql = First(keyArgs, "sql", "query", "statement");
                var hint = SqlHint(sql);
                return string.IsNullOrEmpty(hint)
                    ? "Querying the database for the rows needed by this analysis."
                    : "Querying the database: " + hint + ".";
            }

            if (Is(name, "data_analyze"))
            {
                var ds = First(keyArgs, "dataset_name", "datasetName", "name");
                return string.IsNullOrEmpty(ds)
                    ? "Summarizing / reshaping the cached dataset for the requested breakdowns."
                    : "Analyzing dataset \"" + Clip(ds, 40) + "\" for the requested breakdowns.";
            }

            if (Is(name, "render_interactive", "render_none_interactive", "data_render"))
            {
                var layout = First(keyArgs, "layout", "ui", "mode");
                return string.IsNullOrEmpty(layout)
                    ? "Building the Result visualization from the analyzed data."
                    : "Building Result view (layout=" + Clip(layout, 24) + ") from the analyzed data.";
            }

            if (Is(name, "call_agent"))
            {
                var target = First(keyArgs, "targetSkillKey", "TargetSkillKey") ?? toolLabel;
                var msg = First(keyArgs, "message", "Message");
                return string.IsNullOrEmpty(msg)
                    ? "Delegating to skill \"" + Clip(target, 40) + "\" for a specialized step."
                    : "Delegating to \"" + Clip(target, 40) + "\": " + Clip(msg, 100) + ".";
            }

            // Generic fallback — still include the decisive arg if any
            var why = ToolPurpose(name);
            var argBit = ShortArgSummary(keyArgs);
            if (!string.IsNullOrEmpty(argBit))
                return why + " (" + argBit + ").";
            return why + ".";
        }

        public static string OutcomeAfterTool(string toolName, KernelArguments args, string resultText, bool ok)
        {
            var name = (toolName ?? "").Trim();
            // Soft failures: tool returns JSON { IsSuccess:false, Message } without throwing.
            var softFail = !ok || IsSoftFailure(resultText);
            if (softFail)
            {
                var err = Clip(SoftFailureMessage(resultText) ?? FirstMeaningfulLine(resultText), 120);
                return string.IsNullOrEmpty(err)
                    ? "`" + name + "` failed — will try another path."
                    : "`" + name + "` failed (" + err + ") — will try another path.";
            }

            if (IsAskUser(name))
                return "Waiting for the user's answer before continuing.";

            if (Is(name, "api_provider_search"))
            {
                var hits = SummarizeSearchHits(resultText);
                return string.IsNullOrEmpty(hits)
                    ? "API search returned no clear match — may need schema or a different keyword."
                    : "API search found: " + hits + " — will open the best match for details.";
            }

            if (Is(name, "api_provider_detail", "api_provider_get"))
            {
                var hint = SummarizeDetail(resultText);
                return string.IsNullOrEmpty(hint)
                    ? "Got API details — ready to execute with the required parameters."
                    : "Got API details (" + hint + ") — ready to execute.";
            }

            if (Is(name, "api_provider_execute") || Is(name, "execute_sql", "run_sql", "sql_query"))
            {
                var rows = CountRowsHint(resultText);
                return string.IsNullOrEmpty(rows)
                    ? "Data returned — next: break down by the requested dimensions."
                    : "Got " + rows + " — next: break down by the requested dimensions.";
            }

            if (Is(name, "get_database_schema", "database_schema", "schema_meta"))
            {
                var cols = SummarizeSchemaColumns(resultText, new[] { "classification", "size", "sample", "status", "style" });
                return string.IsNullOrEmpty(cols)
                    ? "Schema loaded — checking which columns match classification / size range / sample status."
                    : "Schema has relevant columns: " + cols + " — enough to group by the three dimensions.";
            }

            if (Is(name, "data_analyze"))
                return "Analysis ready — can render KPIs / charts or ask the user how to present it.";

            if (Is(name, "render_interactive", "render_none_interactive", "data_render"))
                return "Visualization posted to Result.";

            var brief = Clip(FirstMeaningfulLine(resultText), 100);
            return string.IsNullOrEmpty(brief)
                ? "`" + name + "` succeeded — using this to decide the next step."
                : "`" + name + "` → " + brief;
        }

        public static string Opening(string userMessage)
        {
            var u = (userMessage ?? "").Trim();
            if (string.IsNullOrEmpty(u))
                return "Reading the request and deciding which tools can answer it.";
            var one = Clip(u.Replace("\r\n", " ").Replace("\n", " "), 160);
            return "Request: \"" + one + "\". Plan: locate the right data source, pull rows, then break down as asked.";
        }

        // ── helpers ──────────────────────────────────────────────────────────

        /// <summary>True when result JSON reports failure without an exception (common for execute_sql).</summary>
        public static bool IsSoftFailure(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText)) return false;
            try
            {
                var o = JToken.Parse(resultText) as JObject;
                if (o == null) return false;
                var ok = o["IsSuccess"] ?? o["isSuccess"] ?? o["ok"] ?? o["success"];
                if (ok != null && ok.Type == JTokenType.Boolean && !ok.Value<bool>())
                    return true;
                if (ok != null && ok.Type == JTokenType.String
                    && bool.TryParse(ok.ToString(), out var b) && !b)
                    return true;
            }
            catch { /* not JSON */ }
            return false;
        }

        private static string SoftFailureMessage(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText)) return null;
            try
            {
                var o = JToken.Parse(resultText) as JObject;
                if (o == null) return null;
                var msg = o["Message"]?.ToString()
                    ?? o["message"]?.ToString()
                    ?? o["error"]?.ToString()
                    ?? o["Error"]?.ToString();
                return string.IsNullOrWhiteSpace(msg) ? null : msg.Trim();
            }
            catch { return null; }
        }

        private static bool IsAskUser(string name) =>
            string.Equals(name, "ask_user", StringComparison.OrdinalIgnoreCase);

        private static bool Is(string name, params string[] options) =>
            options.Any(o => string.Equals(name, o, StringComparison.OrdinalIgnoreCase));

        private static string NarrateAskUserIntent(Dictionary<string, string> keyArgs)
        {
            var ctx = First(keyArgs, "contextKey", "ContextKey") ?? "";
            var prompt = First(keyArgs, "prompt", "Prompt", "question", "title", "Title") ?? "";
            var mode = First(keyArgs, "mode", "Mode", "ui", "Ui") ?? "";

            if (ctx.IndexOf("kpi", StringComparison.OrdinalIgnoreCase) >= 0
                || ctx.IndexOf("dashboard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Interactive KPI/dashboard requires user confirmation (gate: "
                    + (string.IsNullOrEmpty(ctx) ? "kpi_dashboard" : Clip(ctx, 40))
                    + ") before render — asking the user to choose.";
            }

            if (!string.IsNullOrEmpty(prompt))
            {
                var why = string.IsNullOrEmpty(mode) ? "" : " [" + Clip(mode, 20) + "]";
                return "Need a user decision" + why + " before continuing: \"" + Clip(prompt, 120) + "\".";
            }

            return "Ambiguous or gated step — asking the user before proceeding"
                + (string.IsNullOrEmpty(ctx) ? "." : " (context=" + Clip(ctx, 40) + ").");
        }

        private static string ToolPurpose(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Calling a tool";
            return "Calling `" + name + "` for the next piece of evidence";
        }

        private static Dictionary<string, string> KeyArgs(KernelArguments args)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (args == null) return map;
            foreach (var kv in args)
            {
                if (kv.Value == null) continue;
                var s = kv.Value as string ?? kv.Value.ToString();
                if (string.IsNullOrWhiteSpace(s)) continue;
                map[kv.Key] = s.Trim();
            }
            return map;
        }

        private static string First(Dictionary<string, string> map, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (map.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v;
            }
            return null;
        }

        private static string ShortArgSummary(Dictionary<string, string> map)
        {
            if (map == null || map.Count == 0) return null;
            foreach (var k in new[] { "query", "keyword", "table", "tableName", "dataset_name", "sql", "layout", "path", "name" })
            {
                if (map.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return k + "=" + Clip(v, 50);
            }
            var first = map.FirstOrDefault(kv => kv.Value.Length < 80);
            return string.IsNullOrEmpty(first.Key) ? null : first.Key + "=" + Clip(first.Value, 40);
        }

        private static string SqlHint(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return null;
            var s = sql.Trim().Replace("\r\n", " ").Replace("\n", " ");
            // Prefer FROM table name
            var m = System.Text.RegularExpressions.Regex.Match(s, @"\bfrom\s+([\[\]\w\.]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return "FROM " + Clip(m.Groups[1].Value, 40);
            return Clip(s, 80);
        }

        private static string SummarizeSearchHits(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText)) return null;
            try
            {
                var token = JToken.Parse(resultText);
                var arr = token as JArray
                    ?? token["items"] as JArray
                    ?? token["data"] as JArray
                    ?? token["results"] as JArray
                    ?? token["apis"] as JArray;
                if (arr == null || arr.Count == 0) return null;
                var names = new List<string>();
                foreach (var item in arr.Take(3))
                {
                    var n = item?["name"]?.ToString()
                        ?? item?["Name"]?.ToString()
                        ?? item?["display"]?.ToString()
                        ?? item?["Display"]?.ToString()
                        ?? item?["title"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(n)) names.Add(Clip(n.Trim(), 40));
                }
                var more = arr.Count > names.Count ? " (+" + (arr.Count - names.Count) + " more)" : "";
                if (names.Count == 0) return arr.Count + " match(es)";
                return string.Join(", ", names) + more;
            }
            catch
            {
                return null;
            }
        }

        private static string SummarizeDetail(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText)) return null;
            try
            {
                var o = JToken.Parse(resultText) as JObject;
                if (o == null) return null;
                var name = o["name"]?.ToString() ?? o["Name"]?.ToString() ?? o["apiName"]?.ToString();
                var type = o["apiType"]?.ToString() ?? o["ApiType"]?.ToString() ?? o["type"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(type))
                    return Clip(name, 40) + ", type=" + Clip(type, 24);
                if (!string.IsNullOrWhiteSpace(name)) return Clip(name, 50);
            }
            catch { /* ignore */ }
            return null;
        }

        private static string SummarizeSchemaColumns(string resultText, string[] prefer)
        {
            if (string.IsNullOrWhiteSpace(resultText)) return null;
            try
            {
                var token = JToken.Parse(resultText);
                var cols = new List<string>();
                CollectColumnNames(token, cols);
                if (cols.Count == 0) return null;
                var hit = cols
                    .Where(c => prefer.Any(p => c.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList();
                if (hit.Count > 0) return string.Join(", ", hit);
                return string.Join(", ", cols.Take(4)) + (cols.Count > 4 ? "…" : "");
            }
            catch
            {
                return null;
            }
        }

        private static void CollectColumnNames(JToken token, List<string> cols)
        {
            if (token == null || cols.Count > 40) return;
            if (token is JObject obj)
            {
                foreach (var p in obj.Properties())
                {
                    if (p.Name.Equals("columns", StringComparison.OrdinalIgnoreCase)
                        || p.Name.Equals("Fields", StringComparison.OrdinalIgnoreCase)
                        || p.Name.Equals("fields", StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Value is JArray arr)
                        {
                            foreach (var item in arr)
                            {
                                var n = item?["name"]?.ToString()
                                    ?? item?["Name"]?.ToString()
                                    ?? item?["columnName"]?.ToString()
                                    ?? (item?.Type == JTokenType.String ? item.ToString() : null);
                                if (!string.IsNullOrWhiteSpace(n)) cols.Add(n.Trim());
                            }
                        }
                    }
                    else CollectColumnNames(p.Value, cols);
                }
            }
            else if (token is JArray a)
            {
                foreach (var item in a.Take(20)) CollectColumnNames(item, cols);
            }
        }

        private static string CountRowsHint(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText)) return null;
            try
            {
                var token = JToken.Parse(resultText);
                if (token is JArray arr) return arr.Count + " row(s)";
                var n = token["rowCount"] ?? token["RowCount"] ?? token["count"] ?? token["total"];
                if (n != null) return n + " row(s)";
                var data = token["data"] as JArray ?? token["rows"] as JArray ?? token["records"] as JArray;
                if (data != null) return data.Count + " row(s)";
            }
            catch { /* ignore */ }
            // CSV-ish
            var lines = resultText.Split('\n');
            if (lines.Length > 2 && lines[0].Contains(',')) return (lines.Length - 1) + " row(s)";
            return null;
        }

        private static string FirstMeaningfulLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            foreach (var line in text.Split('\n'))
            {
                var t = line.Trim();
                if (t.Length == 0 || t == "{" || t == "[" || t == "null") continue;
                return t;
            }
            return null;
        }

        private static string Clip(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace("\r\n", " ").Replace("\n", " ").Trim();
            if (s.Length <= max) return s;
            return s.Substring(0, Math.Max(1, max - 1)) + "…";
        }
    }
}
