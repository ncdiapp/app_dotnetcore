using System;
using System.Collections.Generic;
using System.Linq;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// A short row list that matches one chat cache is replaced by that cache.
    /// Two matches and no dataset_name is an error.
    /// </summary>
    public static class CacheRowExpand
    {
        public static JArray Expand(string chatSessionKey, JArray inline)
        {
            if (inline == null || inline.Count == 0 || string.IsNullOrWhiteSpace(chatSessionKey))
                return inline;
            var inlineRows = inline.OfType<JObject>().ToList();
            if (inlineRows.Count == 0)
                return inline;

            string winner = null;
            JArray winnerRows = null;
            foreach (var name in AgentDataAnalyzeCacheBL.ListNames(chatSessionKey))
            {
                if (!AgentDataAnalyzeCacheBL.TryExportDataJson(chatSessionKey, name, out var dataJson, out _, out _))
                    continue;
                var cached = JArray.Parse(dataJson);
                var cachedRows = cached.OfType<JObject>().ToList();
                if (cachedRows.Count <= inlineRows.Count)
                    continue;
                if (!Matches(inlineRows, cachedRows))
                    continue;
                if (winner != null)
                    throw new InvalidOperationException("Two cached datasets match this content. Pass dataset_name.");
                winner = name;
                winnerRows = cached;
            }
            return winnerRows ?? inline;
        }

        public static bool Matches(IReadOnlyList<JObject> inlineRows, IReadOnlyList<JObject> cachedRows)
        {
            if (inlineRows == null || inlineRows.Count == 0 || cachedRows == null || cachedRows.Count == 0)
                return false;
            var bestHits = 0;
            var bestValues = 0;
            foreach (var prop in inlineRows[0].Properties())
            {
                var values = inlineRows
                    .Select(o => o[prop.Name]?.ToString() ?? "")
                    .Where(v => v.Length > 0)
                    .ToList();
                if (values.Count == 0)
                    continue;
                foreach (var column in cachedRows[0].Properties())
                {
                    var set = new HashSet<string>(
                        cachedRows.Select(o => o[column.Name]?.ToString() ?? ""),
                        StringComparer.OrdinalIgnoreCase);
                    var hits = values.Count(v => set.Contains(v));
                    if (hits > bestHits)
                    {
                        bestHits = hits;
                        bestValues = values.Count;
                    }
                }
            }
            return bestValues > 0 && bestHits >= Math.Max(1, bestValues / 2);
        }
    }
}
