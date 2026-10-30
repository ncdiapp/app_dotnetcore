using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    public static class AgentDisplayRowLimitBL
    {
        public const int MaxTurnIntentRows = 50000;

        private static readonly ConcurrentDictionary<string, int> TurnIntentBySession =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, int> LimitBySessionDataset =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] LimitArgKeys =
        {
            "top", "limit", "rowcount", "row_count", "maxrows", "max_rows", "take", "pagesize", "page_size",
            "count", "topn", "rowlimit", "row_limit", "displaylimit", "display_limit"
        };

        public static void SetTurnRowIntent(string chatSessionKey, int? rows)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            var k = chatSessionKey.Trim();
            if (rows is > 0 and <= MaxTurnIntentRows)
                TurnIntentBySession[k] = rows.Value;
            else
                TurnIntentBySession.TryRemove(k, out _);
        }

        public static bool TryGetTurnRowIntent(string chatSessionKey, out int rows)
        {
            rows = 0;
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return false;
            return TurnIntentBySession.TryGetValue(chatSessionKey.Trim(), out rows) && rows > 0;
        }

        public static void SetDatasetDisplayLimit(string chatSessionKey, string datasetName, int? limit)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(datasetName)) return;
            var key = SessionDatasetKey(chatSessionKey, datasetName);
            if (limit is > 0 and <= MaxTurnIntentRows)
                LimitBySessionDataset[key] = limit.Value;
            else
                LimitBySessionDataset.TryRemove(key, out _);
        }

        public static bool TryGetDatasetDisplayLimit(string chatSessionKey, string datasetName, out int limit)
        {
            limit = 0;
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(datasetName)) return false;
            return LimitBySessionDataset.TryGetValue(SessionDatasetKey(chatSessionKey, datasetName), out limit) && limit > 0;
        }

        public static void ClearSession(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            var prefix = chatSessionKey.Trim() + "\u0001";
            TurnIntentBySession.TryRemove(chatSessionKey.Trim(), out _);
            foreach (var k in LimitBySessionDataset.Keys)
            {
                if (k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    LimitBySessionDataset.TryRemove(k, out _);
            }
        }

        public static void CopySession(string sourceChatSessionKey, string targetChatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(sourceChatSessionKey) || string.IsNullOrWhiteSpace(targetChatSessionKey))
                return;
            var src = sourceChatSessionKey.Trim();
            var dst = targetChatSessionKey.Trim();
            if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase)) return;
            if (TurnIntentBySession.TryGetValue(src, out var turn))
                TurnIntentBySession[dst] = turn;
            var prefix = src + "\u0001";
            foreach (var kv in LimitBySessionDataset)
            {
                if (!kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                var datasetName = kv.Key.Substring(prefix.Length);
                if (string.IsNullOrWhiteSpace(datasetName)) continue;
                LimitBySessionDataset[SessionDatasetKey(dst, datasetName)] = kv.Value;
            }
        }

        /// <summary>
        /// LLM extracts TOP N from this turn's user text. Regex is fallback if the model fails.
        /// Layout like 4x5 / 4 per row is not a row limit.
        /// </summary>
        public static async Task ResolveTurnRowIntentAsync(
            string chatSessionKey,
            string userMessage,
            Kernel kernel,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            var fromRegex = TryParseFromUserMessage(userMessage);
            var fromLlm = await TryParseTopNWithLlmAsync(userMessage, kernel, ct).ConfigureAwait(false);
            SetTurnRowIntent(chatSessionKey, fromLlm ?? fromRegex);
        }

        public static async Task<int?> TryParseTopNWithLlmAsync(
            string userMessage,
            Kernel kernel,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userMessage) || kernel == null) return null;
            try
            {
                var chat = kernel.GetRequiredService<IChatCompletionService>();
                var history = new ChatHistory();
                history.AddSystemMessage(
                    "Extract how many rows/items/records the user wants fetched or shown this turn (SQL TOP N / LIMIT). " +
                    "Return JSON only: {\"topN\": <positive integer>} or {\"topN\": null}. " +
                    "Rules: " +
                    "topN is the display/fetch count (e.g. show me 20 images → 20; 先给我50条 → 50; first twenty → 20). " +
                    "Ignore card/grid layout (4x5, 4 per row, per page columns/rows) unless that number is clearly the item count. " +
                    "If they want all rows, or there is no count, topN is null. " +
                    "Do not invent a default like 100.");
                history.AddUserMessage(userMessage.Trim());
                var raw = await chat.GetChatMessageContentAsync(history, cancellationToken: ct).ConfigureAwait(false);
                return ParseTopNJson(raw?.Content);
            }
            catch
            {
                return null;
            }
        }

        private static int? ParseTopNJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var raw = text.Trim();
            var fence = Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (fence.Success) raw = fence.Groups[1].Value.Trim();
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            try
            {
                var obj = JObject.Parse(raw.Substring(start, end - start + 1));
                var tok = obj["topN"] ?? obj["TopN"] ?? obj["top_n"] ?? obj["n"];
                if (tok == null || tok.Type == JTokenType.Null) return null;
                if (!int.TryParse(tok.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    return null;
                if (n <= 0 || n > MaxTurnIntentRows) return null;
                return n;
            }
            catch
            {
                return null;
            }
        }

        public static int? TryParseFromUserMessage(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return null;
            var t = userMessage.Trim();
            var patterns = new[]
            {
                @"(?i)\b(?:get|show|fetch|list|display|return)(?:\s+me)?\s+(\d{1,6})\b",
                @"(?i)\bgive\s+me\s+(\d{1,6})\b",
                @"(?i)\b(\d{1,6})\s+(?:images?|rows?|records?|items?|files?|cards?)\b",
                @"(?i)\b(\d{1,6})\s+(?:latest|newest|recent|first)\b",
                @"(?i)\btop\s+(\d{1,6})\b",
                @"(?i)\bfirst\s+(\d{1,6})\b",
            };
            foreach (var p in patterns)
            {
                var m = Regex.Match(t, p);
                if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                    && n > 0 && n <= MaxTurnIntentRows)
                    return n;
            }
            return null;
        }

        public static int? TryParseFromStringDictionary(IReadOnlyDictionary<string, string> args)
        {
            if (args == null || args.Count == 0) return null;
            foreach (var key in LimitArgKeys)
            {
                foreach (var kv in args)
                {
                    if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                    if (TryParsePositiveInt(kv.Value, out var n)) return n;
                }
            }
            return null;
        }

        public static int? TryParseFromArgsJson(string argsJson)
        {
            if (string.IsNullOrWhiteSpace(argsJson)) return null;
            try
            {
                if (JToken.Parse(argsJson) is not JObject obj) return null;
                foreach (var prop in obj.Properties())
                {
                    foreach (var key in LimitArgKeys)
                    {
                        if (!string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase)) continue;
                        if (TryParsePositiveInt(prop.Value?.ToString(), out var n)) return n;
                    }
                }
            }
            catch { /* ignore */ }
            return null;
        }

        public static int? TryParseFromToolResult(string resultJson)
        {
            if (string.IsNullOrWhiteSpace(resultJson)) return null;
            try
            {
                if (JToken.Parse(resultJson) is not JObject obj) return null;
                foreach (var name in new[] { "display_row_limit", "displayRowLimit", "row_limit_cap" })
                {
                    var tok = obj[name];
                    if (tok != null && TryParsePositiveInt(tok.ToString(), out var n)) return n;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        public static int? ResolveForToolExecution(string chatSessionKey, IReadOnlyDictionary<string, string> toolArgs, string argsJson = null)
        {
            var fromArgs = TryParseFromStringDictionary(toolArgs) ?? TryParseFromArgsJson(argsJson);
            if (fromArgs.HasValue) return fromArgs;
            if (TryGetTurnRowIntent(chatSessionKey, out var turn)) return turn;
            return null;
        }

        public static int? ResolveForCache(
            string chatSessionKey,
            string datasetName,
            IReadOnlyDictionary<string, string> toolArgs,
            string toolResultJson)
        {
            var fromArgs = TryParseFromStringDictionary(toolArgs);
            if (fromArgs.HasValue) return fromArgs;
            var fromResult = TryParseFromToolResult(toolResultJson);
            if (fromResult.HasValue) return fromResult;
            if (TryGetTurnRowIntent(chatSessionKey, out var turn)) return turn;
            return null;
        }

        public static int? Coalesce(int? a, int? b)
        {
            if (a is > 0 && b is > 0) return Math.Min(a.Value, b.Value);
            if (a is > 0) return a;
            if (b is > 0) return b;
            return null;
        }

        public static int? Coalesce(int? a, int? b, int? c) => Coalesce(Coalesce(a, b), c);

        private static string SessionDatasetKey(string chatSessionKey, string datasetName) =>
            chatSessionKey.Trim() + "\u0001" + datasetName.Trim();

        private static bool TryParsePositiveInt(string s, out int n)
        {
            n = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n > 0 && n <= MaxTurnIntentRows;
        }
    }
}
