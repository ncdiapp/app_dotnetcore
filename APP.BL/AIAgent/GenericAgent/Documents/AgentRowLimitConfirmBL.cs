using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Chat-session gate for large row fetches. Platform COUNTs first; only asks when the
    /// real total exceeds <see cref="AutoShowMaxRows"/>. Options are built from that count
    /// (no pointless TOP 1000 when total is 1002).
    /// </summary>
    public static class AgentRowLimitConfirmBL
    {
        public const string ContextKey = "confirm_row_limit";
        public const string OptionAll = "all";
        public const string OptionTop100 = "top_100";
        public const string OptionTop1000 = "top_1000";

        /// <summary>
        /// At or below this total, fetch all rows — no ask_user.
        /// Rationale: small enough that TOP 100 vs all is noise; UI can show the full set.
        /// </summary>
        public const int AutoShowMaxRows = 250;

        /// <summary>
        /// Offer TOP 1000 only when total ≥ this (1000 must be a real subset, not ≈ all).
        /// Below this (e.g. 1002) options are TOP 100 vs All N only.
        /// </summary>
        public const int Top1000MinTotal = 1200;

        private static readonly ConcurrentDictionary<string, string> ChoiceBySession =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, long> CountBySession =
            new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        public static void Mark(string chatSessionKey, string optionId)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(optionId)) return;
            var id = NormalizeOption(optionId);
            if (id == null) return;
            ChoiceBySession[chatSessionKey.Trim()] = id;
        }

        public static void RememberCount(string chatSessionKey, long count)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || count < 0) return;
            CountBySession[chatSessionKey.Trim()] = count;
        }

        public static void Clear(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            var k = chatSessionKey.Trim();
            ChoiceBySession.TryRemove(k, out _);
            CountBySession.TryRemove(k, out _);
        }

        public static bool TryGetMaxRows(string chatSessionKey, out int maxRows)
        {
            maxRows = 0;
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return false;
            if (!ChoiceBySession.TryGetValue(chatSessionKey.Trim(), out var v) || string.IsNullOrEmpty(v))
                return false;
            maxRows = MaxRowsForOption(v);
            return maxRows > 0;
        }

        public static bool IsConfirmed(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return false;
            return ChoiceBySession.ContainsKey(chatSessionKey.Trim());
        }

        public static int MaxRowsForOption(string optionId)
        {
            var n = NormalizeOption(optionId);
            if (n == null) return 0;
            if (n.StartsWith("top_", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(n.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var custom)
                && custom > 0)
                return custom;
            return n switch
            {
                OptionAll => AgentDataAnalyzeCacheBL.MaxCachedRows,
                OptionTop100 => 100,
                OptionTop1000 => 1000,
                _ => 0
            };
        }

        public static bool LooksLikeChoice(IEnumerable<string> selectedIds, string freeText, out string optionId)
        {
            optionId = null;
            if (selectedIds != null)
            {
                foreach (var id in selectedIds)
                {
                    var n = NormalizeOption(id);
                    if (n != null)
                    {
                        optionId = n;
                        return true;
                    }
                }
            }
            var fromText = NormalizeOption(freeText);
            if (fromText != null)
            {
                optionId = fromText;
                return true;
            }
            return false;
        }

        public static bool IsRowLimitConfirmKey(string contextKey)
        {
            if (string.IsNullOrWhiteSpace(contextKey)) return false;
            var k = contextKey.Trim();
            return k.Equals(ContextKey, StringComparison.OrdinalIgnoreCase)
                   || k.IndexOf("row_limit", StringComparison.OrdinalIgnoreCase) >= 0
                   || k.IndexOf("confirm_row", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Detail SELECT that may return many rows. Until the user confirms via ask_user,
        /// ANY model-invented TOP/LIMIT is ignored (COUNT first). Only scalar aggregates /
        /// GROUP BY skip this gate. After Mark(), <see cref="TryGetMaxRows"/> applies the choice.
        /// </summary>
        public static bool NeedsRowLimitConfirm(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;
            if (IsLikelyScalarAggregate(sql)) return false;
            if (Regex.IsMatch(sql, @"\bGROUP\s+BY\b", RegexOptions.IgnoreCase)) return false;
            if (!Regex.IsMatch(sql, @"\bFROM\b", RegexOptions.IgnoreCase)) return false;
            return true;
        }

        public static bool HasTopOrLimit(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;
            return Regex.IsMatch(sql, @"\bTOP\s*\(?\s*\d+", RegexOptions.IgnoreCase)
                   || Regex.IsMatch(sql, @"\bLIMIT\s+\d+", RegexOptions.IgnoreCase);
        }

        /// <summary>Model preview habits — still run COUNT + ask_user when total is large.</summary>
        public static bool HasSoftPreviewTopOrLimit(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;
            return Regex.IsMatch(sql, @"\bTOP\s*\(?\s*(20|100|500|1000)\s*\)?", RegexOptions.IgnoreCase)
                   || Regex.IsMatch(sql, @"\bLIMIT\s+(20|100|500|1000)\b", RegexOptions.IgnoreCase);
        }

        /// <summary>Non-soft TOP/LIMIT (user/agent already picked a specific size).</summary>
        public static bool HasHardCustomTopOrLimit(string sql)
        {
            if (!HasTopOrLimit(sql)) return false;
            return !HasSoftPreviewTopOrLimit(sql);
        }

        /// <summary>Remove TOP/LIMIT so COUNT reflects the full result set.</summary>
        public static string StripTopOrLimit(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;
            var s = Regex.Replace(sql, @"\bSELECT\s+TOP\s*\(?\s*\d+\s*\)?\s+", "SELECT ", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\s+LIMIT\s+\d+(\s+OFFSET\s+\d+)?\s*$", "", RegexOptions.IgnoreCase);
            return s;
        }

        /// <summary>
        /// Wrap a detail SELECT as COUNT(*). Strips soft TOP/LIMIT and trailing ORDER BY.
        /// </summary>
        public static string BuildCountSql(string sql)
        {
            var inner = StripTopOrLimit(sql?.Trim() ?? "");
            inner = StripTrailingOrderBy(inner);
            if (inner.EndsWith(";", StringComparison.Ordinal))
                inner = inner.Substring(0, inner.Length - 1).TrimEnd();
            return "SELECT COUNT_BIG(*) AS __agent_row_count FROM (\n" + inner + "\n) AS __agent_count_q";
        }

        public static bool TryParseCount(IList<Dictionary<string, object>> results, out long count)
        {
            count = 0;
            if (results == null || results.Count == 0) return false;
            var row = results[0];
            foreach (var kv in row)
            {
                if (kv.Key != null
                    && kv.Key.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0
                    && kv.Value != null
                    && long.TryParse(Convert.ToString(kv.Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                    return count >= 0;
            }
            // Single-cell fallback
            var first = row.Values.FirstOrDefault();
            if (first != null
                && long.TryParse(Convert.ToString(first, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                return count >= 0;
            return false;
        }

        /// <summary>True when total is small enough to show all without asking.</summary>
        public static bool ShouldAutoShowAll(long totalRows) => totalRows >= 0 && totalRows <= AutoShowMaxRows;

        public static List<(string Id, string Label)> BuildOptions(long totalRows, string chatSessionKey = null)
        {
            var opts = new List<(string, string)>();
            if (!string.IsNullOrWhiteSpace(chatSessionKey)
                && AgentDisplayRowLimitBL.TryGetTurnRowIntent(chatSessionKey, out var turnN)
                && turnN > 0
                && turnN < totalRows
                && turnN != 100
                && turnN != 1000)
            {
                opts.Add(("top_" + turnN.ToString(CultureInfo.InvariantCulture), "TOP " + turnN.ToString(CultureInfo.InvariantCulture)));
            }
            if (totalRows > 100)
                opts.Add((OptionTop100, "TOP 100"));
            if (totalRows >= Top1000MinTotal)
                opts.Add((OptionTop1000, "TOP 1000"));
            var allLabel = totalRows > AgentDataAnalyzeCacheBL.MaxCachedRows
                ? $"All rows (first {AgentDataAnalyzeCacheBL.MaxCachedRows:N0} — platform cache cap)"
                : $"All {totalRows:N0} rows";
            opts.Add((OptionAll, allLabel));
            return opts;
        }

        public static string AskUserOptionsJson(long totalRows, string chatSessionKey = null)
        {
            var arr = BuildOptions(totalRows, chatSessionKey)
                .Select(o => new { id = o.Id, label = o.Label })
                .ToList();
            return JsonConvert.SerializeObject(arr);
        }

        public static bool TryGetHardTopRowLimit(string sql, out int limit)
        {
            limit = 0;
            if (!HasHardCustomTopOrLimit(sql)) return false;
            var m = Regex.Match(sql ?? "", @"\bTOP\s*\(?\s*(\d+)\s*\)?", RegexOptions.IgnoreCase);
            if (m.Success
                && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out limit)
                && limit > 0)
                return true;
            m = Regex.Match(sql ?? "", @"\bLIMIT\s+(\d+)\b", RegexOptions.IgnoreCase);
            if (m.Success
                && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out limit)
                && limit > 0)
                return true;
            limit = 0;
            return false;
        }

        public static string AskUserPrompt(long totalRows) =>
            $"This query would return {totalRows:N0} rows. How many should I fetch and show?";

        public static string SoftFailMessage(long totalRows) =>
            $"Counted {totalRows:N0} rows — user choice required before fetching. " +
            "Call ask_user with contextKey='" + ContextKey +
            "', mode=single_choice, prompt='" + AskUserPrompt(totalRows).Replace("'", "\\'") +
            "', optionsJson=" + AskUserOptionsJson(totalRows) + ". " +
            "Tell the user the row count in the prompt. After they answer, re-run execute_sql " +
            "with TOP matching their choice (omit TOP when they chose all). Do not invent a TOP yourself.";

        /// <summary>Legacy static options — prefer <see cref="AskUserOptionsJson(long)"/>.</summary>
        public static string AskUserOptionsJson() => AskUserOptionsJson(Top1000MinTotal);

        public static string AskUserPromptHint() =>
            "Count the rows first. If total ≤ " + AutoShowMaxRows + ", fetch all without asking. " +
            "If larger, ask_user confirm_row_limit with options based on that count (include the number in the prompt).";

        private static string StripTrailingOrderBy(string sql)
        {
            // Remove final ORDER BY ... (best-effort; does not parse nested queries).
            return Regex.Replace(sql,
                @"\s+ORDER\s+BY\s+[\s\S]*$",
                "",
                RegexOptions.IgnoreCase);
        }

        private static string NormalizeOption(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            if (t is "all" or "full" or "everything" or "show_all" or "all_rows")
                return OptionAll;
            if (t is "top_100" or "top100" or "100" or "top_100_rows")
                return OptionTop100;
            if (t is "top_1000" or "top1000" or "1000" or "top_1000_rows")
                return OptionTop1000;
            if (t.StartsWith("top_", StringComparison.Ordinal)
                && int.TryParse(t.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var customTop)
                && customTop > 0)
                return "top_" + customTop.ToString(CultureInfo.InvariantCulture);
            if (t.Contains("all") && (t.Contains("row") || t.Contains("data") || t.Contains("show")))
                return OptionAll;
            if (t.Contains("1000"))
                return OptionTop1000;
            if (Regex.IsMatch(t, @"\b100\b") && !t.Contains("1000"))
                return OptionTop100;
            return null;
        }

        private static bool IsLikelyScalarAggregate(string sql)
        {
            if (Regex.IsMatch(sql, @"\bGROUP\s+BY\b", RegexOptions.IgnoreCase))
                return false;
            return Regex.IsMatch(sql,
                @"^\s*SELECT\s+(DISTINCT\s+)?(COUNT|SUM|AVG|MIN|MAX)\s*\(",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
    }
}
