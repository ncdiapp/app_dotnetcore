using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace App.BL.AppDataIntegrationAgent
{
    /// <summary>
    /// Server-side cache for full MCP probe query results. Agent receives summaries only.
    /// </summary>
    public static class AppDataIntegrationProbeCacheBL
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> BySession
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private const int MaxEntriesPerSession = 120;

        public static string Store(string sessionId, string sql, DataTable dt)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || dt == null) return null;
            var key = BuildKey(sql);
            var bucket = BySession.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            if (bucket.Count >= MaxEntriesPerSession)
            {
                // Drop oldest arbitrary entry when over cap.
                foreach (var k in bucket.Keys)
                {
                    bucket.TryRemove(k, out _);
                    break;
                }
            }
            var rows = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn col in dt.Columns)
                {
                    var v = row[col];
                    dict[col.ColumnName] = v == DBNull.Value ? null : v;
                }
                rows.Add(dict);
            }
            var payload = JsonConvert.SerializeObject(new
            {
                RowCount = dt.Rows.Count,
                Columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                Rows = rows
            });
            bucket[key] = payload;
            return key;
        }

        public static void ClearSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;
            BySession.TryRemove(sessionId, out _);
        }

        private static string BuildKey(string sql)
        {
            var norm = (sql ?? "").Trim();
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(norm));
                return "probe_" + BitConverter.ToString(hash).Replace("-", "").Substring(0, 16).ToLowerInvariant();
            }
        }
    }
}
